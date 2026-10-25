#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# Comprobaciones mecanicas de /revisar sobre los ficheros cambiados.
#
# Busca patrones que AGENTS.md y docs/ prohiben de forma explicita. Es
# deterministico y reproducible: lo mismo detecta hoy que en la Fase 19.
#
# Salida, una linea por hallazgo:
#   SEVERIDAD|ID|fichero:linea|descripcion (regla)
# y una ultima linea:
#   RESUMEN|bloqueantes=N|importantes=M
#
# Codigo de salida: 1 si hay algun BLOQUEANTE, 0 en otro caso.
# Este fichero DEBE tener finales de linea LF (ver .gitattributes).
# -----------------------------------------------------------------------------

set -u
cd "${CLAUDE_PROJECT_DIR:-.}" || exit 2

# 1. Ficheros a revisar: cambios respecto a HEAD y ficheros nuevos no ignorados.
#    Sin ningun commit todavia (primera fase), se revisa todo lo que hay.
if git rev-parse --verify -q HEAD > /dev/null 2>&1; then
  CAMBIADOS=$( { git diff --name-only --diff-filter=ACMR HEAD; \
                 git ls-files --others --exclude-standard; } | sort -u )
else
  CAMBIADOS=$( { git ls-files; git ls-files --others --exclude-standard; } | sort -u )
fi

if [ -z "$CAMBIADOS" ]; then
  echo "INFO|-|-|Sin cambios que revisar"
  echo "RESUMEN|bloqueantes=0|importantes=0"
  exit 0
fi

BLOQUEANTES=0
IMPORTANTES=0

informar() {  # severidad id ubicacion descripcion
  echo "$1|$2|$3|$4"
  case "$1" in
    BLOQUEANTE) BLOQUEANTES=$((BLOQUEANTES + 1)) ;;
    IMPORTANTE) IMPORTANTES=$((IMPORTANTES + 1)) ;;
  esac
}

# ficheros_que_coinciden <regex de ruta>
ficheros_que_coinciden() {
  printf '%s\n' "$CAMBIADOS" | grep -E "$1" || true
}

# buscar <severidad> <id> <regex de contenido> <regex de ruta> <descripcion>
buscar() {
  local sev=$1 id=$2 patron=$3 rutas=$4 desc=$5 f linea
  while IFS= read -r f; do
    [ -n "$f" ] && [ -f "$f" ] || continue
    while IFS= read -r linea; do
      informar "$sev" "$id" "$f:${linea%%:*}" "$desc"
    done < <(grep -nE -- "$patron" "$f" 2>/dev/null)
  done < <(ficheros_que_coinciden "$rutas")
}

# tocado <severidad> <id> <regex de ruta> <descripcion>
tocado() {
  local f
  while IFS= read -r f; do
    [ -n "$f" ] && informar "$1" "$2" "$f" "$4"
  done < <(ficheros_que_coinciden "$3")
}

CODIGO='\.(cs|razor)$'
SRC='^src/.*\.(cs|razor)$'

# 2. Bloqueantes: prohibiciones explicitas de AGENTS.md y docs/.
# C01. Se excluyen las migraciones: sus #pragma los genera EF y estan autorizados (arquitectura §3.3).
while IFS= read -r f; do
  [ -n "$f" ] && [ -f "$f" ] || continue
  case "$f" in */Migraciones/*) continue ;; esac
  while IFS= read -r linea; do
    informar BLOQUEANTE C01 "$f:${linea%%:*}" "Supresion con #pragma. Solo .editorconfig justificado (AGENTS §13, A-18)"
  done < <(grep -nE '#pragma[[:space:]]+warning[[:space:]]+disable' "$f" 2>/dev/null)
done < <(ficheros_que_coinciden "$CODIGO")
buscar BLOQUEANTE C02 '<NoWarn>' '\.(csproj|props|targets)$' \
  "Supresion con NoWarn. Solo .editorconfig justificado (AGENTS §13, A-18)"
buscar BLOQUEANTE C03 '<PackageReference[^>]*Version=' '\.csproj$' \
  "Version en .csproj: va en Directory.Packages.props (A-07)"
buscar BLOQUEANTE C04 'DateTime\.(Now|Today)\b|TimeZoneInfo\.Local' "$SRC" \
  "Hora local: usar IRelojSistema.AhoraUtc (AGENTS §9, RN-100)"
buscar BLOQUEANTE C05 'IgnoreQueryFilters|HasQueryFilter' '\.cs$' \
  "Filtro global de EF Core (D-11, AGENTS §3)"
buscar BLOQUEANTE C06 'MarkupString' "$SRC" \
  "MarkupString prohibido en plantillas y ficha publica (RN-171, E-3)"
buscar BLOQUEANTE C07 'lockoutOnFailure:[[:space:]]*false' "$CODIGO" \
  "Bloqueo por intentos fallidos sin efecto (RN-169, R-13)"
# C08. Se ignoran los comentarios: BaseDatosFixture explica por que NO se usa EnsureCreated.
while IFS= read -r f; do
  [ -n "$f" ] && [ -f "$f" ] || continue
  while IFS= read -r linea; do
    informar BLOQUEANTE C08 "$f:${linea%%:*}" "EnsureCreated no aplica la restriccion EXCLUDE (R-3)"
  done < <(grep -nE 'EnsureCreated' "$f" 2>/dev/null | grep -vE '^[0-9]+:[[:space:]]*//')
done < <(ficheros_que_coinciden '\.cs$')
buscar BLOQUEANTE C09 'SaveChanges' '^src/PlataformaReservas\.Infraestructura/Repositorios/.*\.cs$' \
  "SaveChanges en un repositorio: lo llama el caso de uso (arquitectura §9.4)"
buscar BLOQUEANTE C10 '"Habilitado"[[:space:]]*:[[:space:]]*true' 'appsettings[^/]*\.json$' \
  "Correo real habilitado en configuracion versionada (R-7)"

# 3. Importantes: sospechas que requieren mirar el contexto.
buscar IMPORTANTE C11 'async[[:space:]]+void' "$SRC" \
  "async void: el fallo no se observa y puede compartir contexto (arquitectura §9.4)"
buscar IMPORTANTE C12 'Task\.WhenAll' '^src/PlataformaReservas\.Web/.*\.(cs|razor)$' \
  "Paralelismo en un componente: comparte ContextoDatos (R-1, arquitectura §9.4)"
buscar IMPORTANTE C13 '\.Users[[:space:]]*\.[[:space:]]*(Where|First|FirstOrDefault|Single|SingleOrDefault|Any)[[:space:]]*\(' "$SRC" \
  "LINQ sobre Users: los campos cifrados devuelven vacio sin error (RN-167, R-10)"

# C14. Tildes o enies en identificadores de C#. Se eliminan antes comentarios y
#      cadenas, para no marcar textos de interfaz ni documentacion.
while IFS= read -r f; do
  [ -n "$f" ] && [ -f "$f" ] || continue
  while IFS= read -r linea; do
    informar IMPORTANTE C14 "$f:${linea%%:*}" \
      "Posible tilde o enie en un identificador (AGENTS §5). Comprobar a mano"
  done < <(sed -E 's#//.*$##; s#/\*.*\*/##g; s#"([^"\\]|\\.)*"##g' "$f" \
             | grep -nE '[áéíóúüñÁÉÍÓÚÜÑ]' 2>/dev/null)
done < <(ficheros_que_coinciden '\.cs$')

# C18. Un fichero de pruebas por clase probada (AGENTS §11 bis). Pruebas<X>.cs
#      exige una clase X.cs en src/, salvo las transversales de la lista cerrada.
TRANSVERSALES=' Arquitectura Esquema DatosIniciales AislamientoEmpresas ConcurrenciaReservas CifradoEnReposo AutorizacionRutas '
while IFS= read -r f; do
  [ -n "$f" ] && [ -f "$f" ] || continue
  nombre=$(basename "$f" .cs); clase=${nombre#Pruebas}
  case "$TRANSVERSALES" in *" $clase "*) continue ;; esac
  if [ -z "$(find src -name "$clase.cs" -not -path '*/obj/*' -print -quit 2>/dev/null)" ]; then
    informar IMPORTANTE C18 "$f" \
      "No existe $clase.cs en src: ¿agrupa pruebas de varias clases? Uno por clase (AGENTS §11 bis)"
  fi
  if [ "$(grep -cE '^public[[:space:]].*class[[:space:]]+Pruebas' "$f")" -gt 1 ]; then
    informar IMPORTANTE C18 "$f" "Varias clases Pruebas* en un fichero (AGENTS §11 bis)"
  fi
done < <(ficheros_que_coinciden '^tests/[^/]+/(.*/)?Pruebas[A-Z][^/]*\.cs$')

# 4. Ficheros sensibles: su cambio no es un error, pero hay que justificarlo.
tocado IMPORTANTE C15 '^tests/PlataformaReservas\.PruebasUnitarias/Arquitectura/' \
  "Cambian las pruebas de arquitectura. Solo es normal en la Fase 4 (CLAUDE.md)"
tocado IMPORTANTE C16 '(^|/)\.editorconfig$' \
  "Cambia .editorconfig: cada supresion debe figurar en arquitectura §3.3 (A-18)"
tocado IMPORTANTE C17 '(^|/)Directory\.(Build|Packages)\.props$' \
  "Cambian los ajustes comunes: contrastar con arquitectura §2.2, §3.1 y §3.2"

echo "RESUMEN|bloqueantes=$BLOQUEANTES|importantes=$IMPORTANTES"
[ "$BLOQUEANTES" -eq 0 ]
