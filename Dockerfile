FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /fuente

COPY nuget.config Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/PlataformaReservas.Dominio/PlataformaReservas.Dominio.csproj src/PlataformaReservas.Dominio/
COPY src/PlataformaReservas.Aplicacion/PlataformaReservas.Aplicacion.csproj src/PlataformaReservas.Aplicacion/
COPY src/PlataformaReservas.Infraestructura/PlataformaReservas.Infraestructura.csproj src/PlataformaReservas.Infraestructura/
COPY src/PlataformaReservas.Web/PlataformaReservas.Web.csproj src/PlataformaReservas.Web/
RUN dotnet restore src/PlataformaReservas.Web/PlataformaReservas.Web.csproj

COPY src/ src/
RUN dotnet publish src/PlataformaReservas.Web/PlataformaReservas.Web.csproj -c Release -o /app/publicado --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Las claves de Data Protection van a un volumen nombrado: el directorio tiene que existir con el
# propietario del usuario sin privilegios para que el volumen herede el permiso (fase-18.md, duda 5).
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet

COPY --from=compilacion /app/publicado .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlataformaReservas.Web.dll"]
