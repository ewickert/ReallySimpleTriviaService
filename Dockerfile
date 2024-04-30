FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:latest AS builder
ARG TARGETARCH
WORKDIR /src
COPY ./src /src
RUN dotnet publish ReallySimpleTriviaService.csproj -c Release -o dist -a $TARGETARCH

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /src/dist /app
ENTRYPOINT ["/app/ReallySimpleTriviaService"]
CMD [""]
