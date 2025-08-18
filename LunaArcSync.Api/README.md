# 泠月案阁 / LunaArcSync API

这是一个为智能文档管理应用设计的强大、安全且功能丰富的后端服务。它从一个简单的想法出发，逐步构建为一个支持多用户、能够处理复杂图像任务（如 OCR 和图像拼接）的企业级后端系统。

该项目完全使用 .NET 8 和 ASP.NET Core 构建，遵循了整洁架构（Clean Architecture）的最佳实践，确保了代码的高度可维护性、可测试性和可扩展性。

## ✨ 核心功能

*   **🔒 安全的用户认证与授权**:
    *   基于 ASP.NET Core Identity 的完整用户系统（注册/登录）。
    *   使用 JWT (JSON Web Tokens) 进行无状态的 API 认证。
    *   严格的数据隔离，确保用户只能访问自己的数据。

*   **📄 完整的文档与版本管理**:
    *   支持文档的增删改查（CRUD）。
    *   对每一次文件变更（如上传、拼接）都创建新版本，支持版本历史追溯和**版本回滚**。
    *   安全可靠的文件存储，支持基于实体 ID 的规范化命名。

*   **🚀 强大的异步任务处理**:
    *   所有计算密集型任务（OCR、图像拼接）都通过后台任务队列进行**异步处理**，保证 API 快速响应。
    *   提供任务**状态跟踪** API，允许客户端轮询任务进度（排队、处理中、完成、失败）。

*   **🧠 智能图像处理**:
    *   **高精度 OCR**: 使用 Tesseract 引擎，不仅能识别文本，还能返回每个词、每一行的**精确坐标和布局信息**。
    *   **智能图像拼接**: 使用 OpenCV (OpenCvSharp) 将多张有重叠区域的图片拼接成一张无缝的大图。

*   **🔍 高效的数据检索**:
    *   支持基于 OCR 结果的**全文检索**。
    *   检索时自动**忽略空格差异**，提升搜索的容错性和用户体验。
    *   为核心数据列表实现了**分页**，确保在高数据量下的性能。

*   **📖 完善的 API 文档**:
    *   通过 Swagger (OpenAPI) 自动生成交互式 API 文档。
    *   正确配置了 JWT 认证，可以直接在文档页面进行登录和调试受保护的接口。

## 🛠️ 技术栈

*   **框架**: ASP.NET Core 8
*   **数据库**: Entity Framework Core 8 with SQLite (可轻松切换至 SQL Server, PostgreSQL 等)
*   **认证**: ASP.NET Core Identity, JWT Bearer Tokens
*   **图像处理**: OpenCvSharp4
*   **OCR**: Tesseract 5
*   **API 文档**: Swashbuckle.AspNetCore (Swagger)

## 🏛️ 项目架构

本项目采用分层的**整洁架构 (Clean Architecture)**，确保了业务逻辑与外部依赖（如数据库、第三方服务）的解耦。

*   **Core**: 包含项目的核心业务逻辑，如实体（Entities）和接口（Interfaces）。它不依赖于任何其他层。
*   **Infrastructure**: 包含对核心层接口的具体实现。这是所有外部依赖所在的地方，例如：
    *   `Data`: Entity Framework Core 的 `DbContext` 和 `Repository` 实现。
    *   `Services`: 对 `IOcrService`, `IImageProcessingService` 等的具体实现。
    *   `FileStorage`: 文件系统操作。
*   **API (Controllers)**: Web API 的入口层。负责接收 HTTP 请求，调用核心服务，处理 DTO 的映射，并返回响应。它依赖于 `Core` 层。
*   **BackgroundTasks**: 包含 `IHostedService` 的实现，用于处理后台任务队列。

## 🚀 开始使用

### 环境要求

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Visual Studio 2022](https://visualstudio.microsoft.com/) (推荐) 或其他代码编辑器
*   一个 API 测试工具 (如 Postman, 或者使用项目自带的 Swagger UI)

### 安装与运行

1.  **克隆仓库**
    ```bash
    git clone https://github,com/CoWave-Fall/LunaArcSync.Api.git
    cd LunaArcSync.Api
    ```

2.  **配置 Tesseract 语言包 (关键步骤!)**
    *   在项目根目录 (`LunaArcSync.Api/`) 下，创建一个名为 `tessdata` 的文件夹。
    *   从 [tessdata_fast](https://github.com/tesseract-ocr/tessdata_fast) 仓库下载所需的语言包。本项目至少需要：
        *   `eng.traineddata` (英文)
        *   `chi_sim.traineddata` (简体中文)
    *   将这两个 `.traineddata` 文件放入 `tessdata` 文件夹。
    *   在 Visual Studio 的解决方案资源管理器中，右键点击这两个文件 -> **属性** -> 将 **“复制到输出目录”** 设置为 **“如果较新则复制”**。

3.  **配置 `appsettings.json`**
    *   打开 `appsettings.Development.json` 或 `appsettings.json`。
    *   检查 `ConnectionStrings` 是否正确。默认配置会使用 SQLite，在项目运行目录下创建一个 `LunaArcSync.db` 文件。
    *   修改 `JWT` 配置段，特别是 `Secret`，请务必更换为一个你自己的、足够长的密钥！

4.  **应用数据库迁移**
    *   打开 Visual Studio 的 **“工具” > “NuGet 包管理器” > “程序包管理器控制台”**。
    *   运行以下命令来创建数据库和所有表：
        ```powershell
        Update-Database
        ```

5.  **运行项目**
    *   在 Visual Studio 中按 `F5`，或者在项目根目录运行 `dotnet run` 命令。
    *   应用启动后，会自动打开浏览器并导航到 Swagger UI 页面 (`/swagger`)。

## 🗺️ API 接口概览

### Accounts
*   `POST /api/accounts/register`: 注册新用户。
*   `POST /api/accounts/login`: 用户登录，获取 JWT。

### Documents (需认证)
*   `GET /api/documents`: 获取当前用户的所有文档（支持分页）。
*   `GET /api/documents/{id}`: 获取单个文档的详细信息。
*   `POST /api/documents`: 上传图片，创建新文档。
*   `PUT /api/documents/{id}`: 更新文档的元数据（如标题）。
*   `DELETE /api/documents/{id}`: 删除文档及其所有版本和文件。
*   `GET /api/documents/search`: 根据 OCR 文本内容进行全文检索。
*   `POST /api/documents/{id}/revert`: 将文档回滚到指定的历史版本。

### Versions (需认证)
*   `GET /api/documents/{documentId}/versions`: 获取某个文档的所有版本历史。
*   `POST /api/documents/{documentId}/versions`: 上传新图片，为文档创建新版本。

### Jobs (需认证)
*   `GET /api/jobs/{jobId}`: 查询异步任务的当前状态。
*   `POST /api/jobs/ocr/{versionId}`: 提交一个 OCR 任务。
*   `POST /api/jobs/stitch/document/{documentId}`: 提交一个图像拼接任务。

## 🔮 未来展望

*   **客户端开发**: 使用 Flutter 或 .NET MAUI 构建跨平台客户端。
*   **部署**: 将应用容器化 (Docker) 并部署到云服务 (Azure, AWS)。
*   **高级搜索**: 集成 Elasticsearch 实现更强大的搜索功能。
*   **实时通知**: 使用 SignalR 在任务完成后向客户端推送实时通知。

---