#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# Hook "Stop" de Claude Code: compila la solucion al final de cada turno.
#
#   exit 0  compila, o no hay nada que compilar: el turno termina con normalidad.
#   exit 2  no compila: Claude Code NO da el turno por terminado y le pasa el
#           error a Claude para que lo corrija.
#   exit 1  no compila tras MAX_INTENTOS: se muestra el error al usuario y se
#           deja terminar el turno, para no entrar en un bucle infinito.
#
# En Windows, Claude Code ejecuta los hooks con Git Bash.
# Este fichero DEBE tener finales de linea LF (ver .gitattributes).
# -----------------------------------------------------------------------------

set -u

cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0

ESTADO=".claude/hooks/.intentos-compilacion"
MAX_INTENTOS=3

# La entrada JSON del hook no se usa, pero hay que consumirla.
cat > /dev/null

# 1. Antes de la Fase 4 no existe solucion: nada que compilar.
#    Se aceptan .slnx (formato por defecto en el SDK de .NET 10) y .sln.
SOLUCION=$(ls ./*.slnx ./*.sln 2>/dev/null | head -n 1)
if [ -z "$SOLUCION" ]; then
  exit 0
fi

# 2. Si no hay cambios de codigo pendientes, no se compila.
#    Evita pagar una compilacion en turnos de solo documentacion o conversacion.
if git rev-parse --is-inside-work-tree > /dev/null 2>&1; then
  CAMBIOS=$(git status --porcelain -- \
    '*.cs' '*.razor' '*.cshtml' '*.csproj' '*.props' '*.targets' \
    '*.slnx' '*.sln' '*.json' 2>/dev/null)
  if [ -z "$CAMBIOS" ]; then
    rm -f "$ESTADO"
    exit 0
  fi
fi

# 3. Compilar.
if SALIDA=$(dotnet build "$SOLUCION" --nologo -v q 2>&1); then
  rm -f "$ESTADO"
  exit 0
fi

# 4. No compila: contar el intento y decidir si se bloquea el fin de turno.
INTENTOS=$(cat "$ESTADO" 2>/dev/null || echo 0)
INTENTOS=$((INTENTOS + 1))
echo "$INTENTOS" > "$ESTADO"

{
  echo "La solucion no compila (intento $INTENTOS de $MAX_INTENTOS)."
  echo "TreatWarningsAsErrors esta activo: los avisos cuentan como errores."
  echo "Corrige la causa. NO silencies avisos con #pragma, NO toques Directory.Build.props"
  echo "y NO modifiques las pruebas de arquitectura para que pasen (AGENTS.md §13)."
  echo "----"
  echo "$SALIDA" | tail -n 40
} >&2

if [ "$INTENTOS" -ge "$MAX_INTENTOS" ]; then
  rm -f "$ESTADO"
  echo "----" >&2
  echo "Maximo de intentos alcanzado. Se deja terminar el turno: revisa el error a mano." >&2
  exit 1
fi

exit 2
