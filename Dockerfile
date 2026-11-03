# ---------- 阶段 1:编译并发布 ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# 先只复制项目文件并还原依赖,这一层只有依赖变了才会重做(利用构建缓存)
COPY src/Gitlet.csproj src/
RUN dotnet restore src/Gitlet.csproj

COPY src/ src/
RUN dotnet publish src/Gitlet.csproj -c Release -o /app --no-restore

# ---------- 阶段 2:只带运行环境的小镜像 ----------
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .

# Gitlet 在"当前目录"里读写 .gitlet,所以运行时要把本地目录挂载到 /work:
#   docker run --rm -v "$(pwd)":/work gitlet init
WORKDIR /work
ENTRYPOINT ["dotnet", "/app/Gitlet.dll"]
