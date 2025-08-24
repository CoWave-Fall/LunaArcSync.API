# 泠月案阁 / LunaArcSync API 参考文档

本文档为前端开发人员提供了与 LunaArcSync 后端服务交互所需的所有 API 接口信息。

**基础 URL**: 所有 API 路径都相对于服务器的基础 URL。
**认证**: 需要认证的接口必须在 HTTP 请求头中包含一个有效的 JWT 令牌：`Authorization: Bearer {Your_JWT_Token}`。

---

## 1. About (公共接口)

### 1.1 获取应用信息

获取应用的名称、版本和描述等基本信息。

- **Method**: `GET`
- **Path**: `/api/about`
- **认证**: 否

#### 成功响应 (200 OK)

```json
{
  "appName": "LunaArcSync API",
  "version": "0.0.1",
  "description": "A powerful backend for an intelligent page management application.",
  "contact": "https://github,com/CoWave-Fall/LunaArcSync.API"
}
```

---

## 2. Accounts (账户管理)

### 2.1 注册新用户

- **Method**: `POST`
- **Path**: `/api/accounts/register`
- **认证**: 否

#### 请求体

```json
{
  "email": "user@example.com",
  "password": "password123",
  "confirmPassword": "password123"
}
```

#### 成功响应 (200 OK)

```json
{
  "message": "User created successfully!"
}
```

#### 错误响应 (400 Bad Request)

- 如果邮箱已存在:
```json
{
  "message": "User with this email already exists."
}
```
- 如果密码不匹配或不符合要求:
```json
{
    "errors": [
        {
            "code": "PasswordMismatch",
            "description": "Passwords do not match."
        }
    ]
}
```

### 2.2 用户登录

- **Method**: `POST`
- **Path**: `/api/accounts/login`
- **认证**: 否

#### 请求体

```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

#### 成功响应 (200 OK)

```json
{
  "token": "ey...",
  "expiration": "2023-10-28T10:00:00Z",
  "userId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "email": "user@example.com"
}
```

#### 错误响应 (401 Unauthorized)

```json
{
  "message": "Invalid email or password."
}
```

---

## 3. Documents (文档管理)

管理文档元数据和页面结构。

### 3.1 获取文档列表 (分页)

- **Method**: `GET`
- **Path**: `/api/documents`
- **认证**: 是
- **Query Params**:
    - `pageNumber` (可选, 默认: 1)
    - `pageSize` (可选, 默认: 10)

#### 成功响应 (200 OK)

```json
{
  "items": [
    {
      "documentId": "doc_guid_1",
      "title": "项目报告",
      "createdAt": "2023-10-27T10:00:00Z",
      "updatedAt": "2023-10-27T12:00:00Z",
      "pageCount": 5,
      "tags": ["重要", "项目A"],
      "ownerEmail": "admin@example.com" // (仅管理员可见)
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

### 3.2 获取单个文档详情

- **Method**: `GET`
- **Path**: `/api/documents/{id}`
- **认证**: 是

#### 成功响应 (200 OK)

```json
{
  "documentId": "doc_guid_1",
  "title": "项目报告",
  "createdAt": "2023-10-27T10:00:00Z",
  "updatedAt": "2023-10-27T12:00:00Z",
  "tags": ["重要", "项目A"],
  "ownerEmail": "admin@example.com", // (仅管理员可见)
  "pages": [
    {
      "pageId": "page_guid_1",
      "title": "报告封面",
      "createdAt": "2023-10-27T10:01:00Z",
      "updatedAt": "2023-10-27T10:01:00Z",
      "order": 1
    },
    {
      "pageId": "page_guid_2",
      "title": "第一章 - 引言",
      "createdAt": "2023-10-27T10:05:00Z",
      "updatedAt": "2023-10-27T10:05:00Z",
      "order": 2
    }
  ]
}
```

### 3.3 创建一个空的新文档

- **Method**: `POST`
- **Path**: `/api/documents`
- **认证**: 是

#### 请求体

```json
{
  "title": "我的新文档"
}
```

#### 成功响应 (201 Created)

```json
{
  "documentId": "new_doc_guid",
  "title": "我的新文档",
  "createdAt": "2023-10-28T14:00:00Z",
  "updatedAt": "2023-10-28T14:00:00Z",
  "pageCount": 0,
  "tags": []
}
```

### 3.4 更新文档元数据

- **Method**: `PUT`
- **Path**: `/api/documents/{id}`
- **认证**: 是

#### 请求体

```json
{
  "title": "更新后的文档标题",
  "tags": ["重要", "已归档"]
}
```

#### 成功响应 (204 No Content)

### 3.5 删除文档

- **Method**: `DELETE`
- **Path**: `/api/documents/{id}`
- **认证**: 是

#### 成功响应 (204 No Content)

### 3.6 添加页面到文档

- **Method**: `POST`
- **Path**: `/api/documents/{documentId}/pages`
- **认证**: 是

#### 请求体

```json
{
  "pageId": "page_guid_to_add"
}
```

#### 成功响应 (204 No Content)

### 3.7 批量设置页面顺序

- **Method**: `POST`
- **Path**: `/api/documents/{documentId}/pages/reorder/set`
- **认证**: 是

#### 请求体

```json
{
  "pageOrders": [
    { "pageId": "page_guid_1", "order": 1 },
    { "pageId": "page_guid_3", "order": 2 },
    { "pageId": "page_guid_2", "order": 3 }
  ]
}
```

#### 成功响应 (204 No Content)

### 3.8 插入页面到指定位置

- **Method**: `POST`
- **Path**: `/api/documents/{documentId}/pages/reorder/insert`
- **认证**: 是

#### 请求体

```json
{
  "pageId": "page_guid_to_insert",
  "newOrder": 2
}
```

#### 成功响应 (204 No Content)

### 3.9 获取用户统计

- **Method**: `GET`
- **Path**: `/api/documents/stats`
- **认证**: 是

#### 成功响应 (200 OK)

```json
{
  "totalDocuments": 15,
  "totalPages": 123
}
```

### 3.10 导出我的数据

- **Method**: `GET`
- **Path**: `/api/documents/my-data-export`
- **认证**: 是

#### 成功响应 (200 OK)
- 返回一个 `application/json` 文件。

---

## 4. Pages (页面管理)

管理核心内容单元——页面。所有图片和内容都附着在页面上。

### 4.1 获取未分配的页面 (分页)

- **Method**: `GET`
- **Path**: `/api/pages/unassigned`
- **认证**: 是

#### 成功响应 (200 OK)
- 返回一个 `PageDto` 数组。
```json
[
    {
      "pageId": "page_guid_1",
      "title": "未分配的扫描件",
      "createdAt": "2023-10-27T10:01:00Z",
      "updatedAt": "2023-10-27T10:01:00Z",
      "order": 0
    }
]
```

### 4.2 获取单个页面详情

- **Method**: `GET`
- **Path**: `/api/pages/{id}`
- **认证**: 是

#### 成功响应 (200 OK)

```json
{
  "pageId": "page_guid_1",
  "title": "报告封面",
  "createdAt": "2023-10-27T10:01:00Z",
  "updatedAt": "2023-10-27T10:01:00Z",
  "totalVersions": 2,
  "currentVersion": {
    "versionId": "version_guid_2",
    "versionNumber": 2,
    "message": "更新了图片清晰度",
    "createdAt": "2023-10-27T11:00:00Z",
    "ocrResult": {
        "lines": [
            {
                "words": [
                    {
                        "text": "泠月案阁",
                        "bbox": { "x1": 10, "y1": 10, "x2": 100, "y2": 40 },
                        "confidence": 95.5
                    }
                ],
                "text": "泠月案阁",
                "bbox": { "x1": 10, "y1": 10, "x2": 100, "y2": 40 }
            }
        ],
        "imageWidth": 800,
        "imageHeight": 600
    }
  }
}
```

### 4.3 上传图片创建新页面

- **Method**: `POST`
- **Path**: `/api/pages`
- **认证**: 是
- **Content-Type**: `multipart/form-data`

#### 请求体 (Form Data)
- `title`: (string) "扫描的合同"
- `file`: (file) a.jpg

#### 成功响应 (201 Created)

```json
{
  "pageId": "new_page_guid",
  "title": "扫描的合同",
  "createdAt": "2023-10-28T15:00:00Z",
  "updatedAt": "2023-10-28T15:00:00Z"
}
```

### 4.4 更新页面标题

- **Method**: `PUT`
- **Path**: `/api/pages/{id}`
- **认证**: 是

#### 请求体

```json
{
  "title": "更新后的页面标题"
}
```

#### 成功响应 (204 No Content)

### 4.5 删除页面

- **Method**: `DELETE`
- **Path**: `/api/pages/{id}`
- **认证**: 是

#### 成功响应 (204 No Content)

### 4.6 全文检索页面

- **Method**: `GET`
- **Path**: `/api/pages/search`
- **认证**: 是
- **Query Params**:
    - `q`: (string, 必须) "搜索关键词"
    - `pageNumber` (可选, 默认: 1)
    - `pageSize` (可选, 默认: 10)

#### 成功响应 (200 OK)
- 返回一个 `PagedResultDto<PageDto>` 对象，结构同 3.1。

### 4.7 回滚页面到指定版本

- **Method**: `POST`
- **Path**: `/api/pages/{id}/revert`
- **认证**: 是

#### 请求体

```json
{
  "targetVersionId": "version_guid_to_revert_to"
}
```

#### 成功响应 (204 No Content)

---

## 5. Versions (版本管理)

### 5.1 获取页面的所有版本

- **Method**: `GET`
- **Path**: `/api/pages/{pageId}/versions`
- **认证**: 是

#### 成功响应 (200 OK)
- 返回一个 `VersionDto` 数组。
```json
[
  {
    "versionId": "version_guid_1",
    "versionNumber": 1,
    "message": "初始版本",
    "createdAt": "2023-10-27T10:01:00Z",
    "ocrResult": null
  },
  {
    "versionId": "version_guid_2",
    "versionNumber": 2,
    "message": "更新了图片清晰度",
    "createdAt": "2023-10-27T11:00:00Z",
    "ocrResult": { ... }
  }
]
```

### 5.2 为页面创建新版本

- **Method**: `POST`
- **Path**: `/api/pages/{pageId}/versions`
- **认证**: 是
- **Content-Type**: `multipart/form-data`

#### 请求体 (Form Data)
- `message`: (string, 可选) "修正了图片倾斜"
- `file`: (file) b.png

#### 成功响应 (201 Created)

```json
{
  "versionId": "new_version_guid",
  "versionNumber": 3,
  "message": "修正了图片倾斜",
  "createdAt": "2023-10-28T16:00:00Z"
}
```

---

## 6. Images (图片获取)

### 6.1 获取指定版本的图片

- **Method**: `GET`
- **Path**: `/api/images/{versionId}`
- **认证**: 是

#### 成功响应 (200 OK)
- 返回图片文件流，如 `image/png` 或 `image/jpeg`。

---

## 7. Jobs (后台任务)

### 7.1 查询任务状态

- **Method**: `GET`
- **Path**: `/api/jobs/{jobId}`
- **认证**: 是

#### 成功响应 (200 OK)

```json
{
  "jobId": "job_guid_1",
  "type": "Ocr", // "Ocr" 或 "Stitch"
  "status": "Completed", // "Queued", "Processing", "Completed", "Failed"
  "associatedPageId": "page_guid_1",
  "submittedAt": "2023-10-28T17:00:00Z",
  "startedAt": "2023-10-28T17:00:01Z",
  "completedAt": "2023-10-28T17:00:05Z",
  "errorMessage": null
}
```

### 7.2 提交 OCR 任务

- **Method**: `POST`
- **Path**: `/api/jobs/ocr/{versionId}`
- **认证**: 是

#### 成功响应 (202 Accepted)

```json
{
  "jobId": "new_ocr_job_guid",
  "message": "OCR job has been queued."
}
```

### 7.3 提交图像拼接任务

- **Method**: `POST`
- **Path**: `/api/jobs/stitch/page/{pageId}`
- **认证**: 是

#### 请求体

```json
{
  "sourceVersionIds": [
    "version_guid_1",
    "version_guid_2",
    "version_guid_3"
  ]
}
```

#### 成功响应 (202 Accepted)

```json
{
  "jobId": "new_stitch_job_guid",
  "message": "Stitch job has been queued."
}
```

---

## 8. Data (数据管理)

**注意**: 以下接口仅限管理员访问。

### 8.1 导出数据库

- **Method**: `GET`
- **Path**: `/api/data/export`
- **认证**: 是 (管理员)

#### 成功响应 (200 OK)
- 返回一个 `application/x-sqlite3` 文件。

### 8.2 导入数据库

- **Method**: `POST`
- **Path**: `/api/data/import`
- **认证**: 是 (管理员)
- **Content-Type**: `multipart/form-data`

#### 请求体 (Form Data)
- `file`: (file) backup.db

#### 成功响应 (200 OK)

```json
"Database imported successfully. Please restart the application for changes to take full effect."
```
