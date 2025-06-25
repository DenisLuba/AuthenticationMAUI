# En

# Firebase Google Auth for .NET MAUI

## ✅ Overview

This template uses FirebaseAuthentication.net and WebAuthenticator. It provides:

* Firebase Hosting (`redirect.html`)
* `AuthenticationMAUI` library for Google Login in .NET MAUI apps. It also implements Email authentication in Firebase

---

## Step-by-step Setup

### 1. Create a Firebase Project

1. Go to (https://console.firebase.google.com)
2. Create a project (e.g., `myapp-auth`)
3. Enable `Authentication > Sign-in method > Google`
4. Note the values:

   * Web API Key (**Project Settings > General > Web API Key**)
   * Auth domain (**Authentication > Settings > Authorized Domains**) — usually `project-id.firebaseapp.com`

### 2. Create OAuth 2.0 Client ID

1. Open [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. If you haven't created one yet, create an `OAuth 2.0 Client ID`:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Copy your `client_id` (in the same place or in Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID)

### 3. Setup Firebase Hosting

1. If not installed, install `firebase-tools' via the terminal [View → Terminal], located in the root directory of the project (first download and install Node.js: https://nodejs.org/en/download/current):

```bash
npm install -g firebase-tools
```

2. Log in:

```bash
firebase login
```

3. Initialize hosting (use your project ID):

```bash
firebase init hosting
```

4. Answer questions from firebase:
5. ```bash
1. Are you ready to proceed? Y
2. Please select an option:
- Add Firebase to an existring Google Cloud Platform project
3. Select the Google Cloud Platform project you would like to add Firebase: your project
4. What do you want to use your public directory? public
5. Configure as a single-page app(rewrite allurls to /index.html)? N
6. Set up authomatic builds and deploys with GitHub? N
```

### 4. Create a file `redirect.html`

In `public/redirect.html`:

```html
<script>
  const token = new URLSearchParams(location.hash.substring(1)).get('id_token');
  const scheme = new URLSearchParams(location.search).get('scheme') || 'myapp';
  if (token) {
    window.location.href = scheme + '://auth?id_token=' + token;
  } else {
    document.body.innerHTML = '<h2>ID Token not found</h2>';
  }
</script>
```

### 5. Edit the file `firebase.json`

```json
{
  "hosting": {
    "public": "public",
    "rewrites": [
      { "source": "/redirect.html", "destination": "/redirect.html" }
    ],
    "ignore": [
      "firebase.json",
      "**/.*",
      "**/node_modules/**"
    ]
  }
}
```

### 6. Deploy

```bash
firebase deploy --only hosting
```

---

### 🔗 Add to your MAUI project

1. Clone the repository:

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. In Visual Studio: Right click on solution → `Add > Existing Project...` → select `AuthenticationMAUI.csproj`
3. Then: Right click on your MAUI project → `Add > Project Reference...` → select `AuthenticationMAUI`

---

## 🌐 Using `FirebaseLoginService`

1. Register `FirebaseLoginData` with DI:

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new ()
        {
            UserStorageService = userStorageService,
            ApiKey = apiKey, // Your Web API Key from Firebase Console (Firebase Console > Project Settings > General > "Web API Key")
            AuthDomain = authDomain, // Usualy it is your-project-id.firebaseapp.com (Firebase Console > Authentication > Settings > "Authorized domains")
            GoogleClientId = googleClientId, // Your Google Client ID (Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID")
            GoogleRedirectUri = googleRedirectUri, // Usualy it is "https://your-project-id.firebaseapp.com/__/auth/handler", but we are changing "__/auth/handler" to "redirect.html",
                                                   // so that it turns out "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // (Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs)
            CallbackScheme = callbackScheme // A callback scheme for Google authentication. For example, "myapp" for myapp:// (but you can also use myapp:// - this will be edited in the constructor)
        });
});
```

2. Add the intent filter for Android `MainActivity.cs`, for example, below the MainActivity class in the same file:

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Android.Content.Intent.ActionView],
    Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // Must match the Callback Scheme (passed to FirebaseLoginService)
}
```

3. Add to `Info.plist` (iOS):

```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>myapp</string>
    </array>
  </dict>
</array>
```

---

This template is reusable for any number of MAUI projects with Firebase Hosting 🔁

# Ru

# Firebase Google Auth for .NET MAUI

## ✅ Обзор

Этот шаблон использует FirebaseAuthentication.net и WebAuthenticator. Он обеспечивает:

* Firebase Hosting (`redirect.html`)
* и библиотеку `AuthenticationMAUI`, которая подключает Google Login в MAUI-приложении. Также в ней реализована аутентификация через Email в Firebase

---

## Пошагово

### 1. Создание Firebase-проекта

1. Перейди в (https://console.firebase.google.com)
2. Создай проект (например, `myapp-auth`)
3. Включи Authentication > Sign-in method > Google
4. Запомни значения:

   * Web API Key (**Project Settings > General > Web API Key**)
   * Auth domain (**Authentication > Settings > Authorized Domains**) — обычно `project-id.firebaseapp.com`

### 2. Создание OAuth 2.0 Client ID

1. Открой [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Создай, если еще не создан, `OAuth 2.0 Client ID`:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Запомни `client_id` (там же или в Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID)

### 3. Настрой firebase hosting

1. Установи, если не установлен, `firebase-tools` через терминал [View → Terminal], находясь в корневой директории проекта (вначале скачай и установи Node.js: https://nodejs.org/en/download/current):

```bash
npm install -g firebase-tools
```

2. Войди:

```bash
firebase login
```

3. Инициализируй hosting (имя проекта бери из Firebase):

```bash
firebase init hosting
```

4. Ответь на вопросы от firebase:
```bash
1. Are you ready to proceed? Y
2. Please select an option:
- Add Firebase to an existring Google Cloud Platform project
3. Select the Google Cloud Platform project you would like to add Firebase: ваш проект
4. What do you want to use your public directory? public
5. Configure as a single-page app(rewrite allurls to /index.html)? N
6. Set up authomatic builds and deploys with GitHub? N
```

### 4. Создай файл redirect.html

`public/redirect.html`:

```html
<script>
  const token = new URLSearchParams(location.hash.substring(1)).get('id_token');
  const scheme = new URLSearchParams(location.search).get('scheme') || 'myapp';
  if (token) {
    window.location.href = scheme + '://auth?id_token=' + token;
  } else {
    document.body.innerHTML = '<h2>ID Token not found</h2>';
  }
</script>
```

### 5. Измени файл firebase.json

```json
{
  "hosting": {
    "public": "public",
    "rewrites": [
      { "source": "/redirect.html", "destination": "/redirect.html" }
    ],
    "ignore": [
      "firebase.json",
      "**/.*",
      "**/node_modules/**"
    ]
  }
}
```

### 6. Деплой

```bash
firebase deploy --only hosting
```

---

### 🔗 Добавление в существующий MAUI проект

1. Клонируй репозиторий:

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. В Visual Studio: ПКМ на решении → `Add > Existing Project...` → выбери `AuthenticationMAUI.csproj`
3. Затем: ПКМ на проекте MAUI → `Add > Project Reference...` → отметь `AuthenticationMAUI`

---

## 🌐 Как использовать FirebaseLoginService

1. Передай FirebaseLoginData через DI в MauiProgram.cs:

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new ()
        {
            UserStorageService = userStorageService,
            ApiKey = apiKey, // Ваш Web API Key из Firebase Console (Firebase Console > Project Settings > General > "Web API Key")
            AuthDomain = authDomain, // Обычно это your-project-id.firebaseapp.com (Firebase Console > Authentication > Settings > "Authorized domains")
            GoogleClientId = googleClientId, // Ваш Google Client ID (Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID")
            GoogleRedirectUri = googleRedirectUri, // Обычно это "https://your-project-id.firebaseapp.com/__/auth/handler", но "__/auth/handler" меняем на "redirect.html",
                                                   // чтобы получилось "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // (Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs)
            CallbackScheme = callbackScheme // Схема обратного вызова для аутентификации через Google. Например, "myapp" для myapp:// (но можно и myapp:// - это будет отредактировано в конструкторе)
        });
});
```

2. Добавь intent-filter для Android `MainActivity.cs`, например, ниже класса MainActivity в том же файле:

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Android.Content.Intent.ActionView],
    Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // Должно совпадать со схемой обратного вызова CallbackScheme (переданной в FirebaseLoginService)
}
```

3. Добавь в Info.plist (для iOS):

```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>myapp</string>
    </array>
  </dict>
</array>
```

---

Успешно! Теперь этот шаблон можно переиспользовать в сотне проектов MAUI с Firebase Hosting!

# Zh
# 用于 .NET MAUI 的 Firebase Google 身份验证
## ✅ 概览
该模板使用 FirebaseAuthentication.net 和 WebAuthenticator，提供以下功能：

* Firebase 托管 (`redirect.html`)

* `AuthenticationMAUI` 库，用于 .NET MAUI 应用中的 Google 登录。它还实现了 Firebase 的电子邮件身份验证功能。

## 分步设置指南
### 1. 创建 Firebase 项目
1. 访问 https://console.firebase.google.com

2. 创建一个项目（例如：`myapp-auth`）

3. 启用 `Authentication > Sign-in method > Google`

4. 记录以下值：

* Web API 密钥（**Project Settings > General > Web API Key**）

* 认证域名（**Authentication > Settings > Authorized Domains**）— 通常是 `project-id.firebaseapp.com`

### 2. 创建 OAuth 2.0 客户端 ID
1. 打开 [**Google Cloud Console > API 与服务 > 凭据**](https://console.cloud.google.com/apis/credentials)

2. 如果尚未创建，请创建一个 `OAuth 2.0 Client ID`：

* 类型：Web 应用

* 授权重定向 URI：`https://project-id.firebaseapp.com/redirect.html`

3. 复制你的 c`lient_id`（在相同页面，或 **Firebase 控制台 > Authentication > Sign-in method > Google > Web SDK 配置 > Web client ID**）

### 3. 设置 Firebase Hosting
1. 如果尚未安装，在项目根目录通过终端安装 firebase-tools（首先需要安装 Node.js：https://nodejs.org/en/download/current）：

```bash
npm install -g firebase-tools
```

2. 登录：

```bash
firebase login
```

3. 初始化托管（使用你的项目 ID）：

```bash
firebase init hosting
```

4. 回答 firebase 提示的问题：
```bash
1. Are you ready to proceed? Y
2. Please select an option:
- Add Firebase to an existring Google Cloud Platform project
3. Select the Google Cloud Platform project you would like to add Firebase: your project
4. What do you want to use your public directory? public
5. Configure as a single-page app(rewrite allurls to /index.html)? N
6. Set up authomatic builds and deploys with GitHub? N
```

### 4. 创建文件 `redirect.html`

放置于 `public/redirect.html`：

```html
<script>
  const token = new URLSearchParams(location.hash.substring(1)).get('id_token');
  const scheme = new URLSearchParams(location.search).get('scheme') || 'myapp';
  if (token) {
    window.location.href = scheme + '://auth?id_token=' + token;
  } else {
    document.body.innerHTML = '<h2>ID Token not found</h2>';
  }
</script>
```

### 5. 编辑文件 firebase.json

```json
{
  "hosting": {
    "public": "public",
    "rewrites": [
      { "source": "/redirect.html", "destination": "/redirect.html" }
    ],
    "ignore": [
      "firebase.json",
      "**/.*",
      "**/node_modules/**"
    ]
  }
}
```

### 6.部署

```bash
firebase deploy --only hosting
```

---

### 🔗 添加到你的 MAUI 项目中
1. 克隆仓库：

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. 在 Visual Studio 中：右键解决方案 → `Add > Existing Project...` → 选择 `AuthenticationMAUI.csproj`

3. 然后：右键你的 MAUI 项目 → `Add > Project Reference...` → 选择 `AuthenticationMAUI`

---

## 🌐 使用 FirebaseLoginService
1. 使用依赖注入注册 `FirebaseLoginData`：
   
```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new ()
        {
            UserStorageService = userStorageService,
            ApiKey = apiKey, // 来自 Firebase 控制台的 Web API Key（Firebase Console > Project Settings > General > "Web API Key"）
            AuthDomain = authDomain, // 通常为 your-project-id.firebaseapp.com（Firebase Console > Authentication > Settings > "Authorized domains"）
            GoogleClientId = googleClientId, // Google 客户端 ID（Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID"）
            GoogleRedirectUri = googleRedirectUri, // 通常为 "https://your-project-id.firebaseapp.com/__/auth/handler"，但我们将 "__/auth/handler" 替换为 "redirect.html"，即
                                                   // "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // （Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client > Authorized redirect URIs）
            CallbackScheme = callbackScheme // Google 登录回调的 scheme。例如 "myapp" 对应 myapp://（可以自定义）
        });
});
```

2. 在 Android 的 MainActivity.cs 中添加 intent filter，例如放在 MainActivity 类之后：

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Android.Content.Intent.ActionView],
    Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // 必须与 FirebaseLoginService 中传递的 Callback Scheme 匹配
}
```

3. 添加到 Info.plist（iOS）：

```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>myapp</string>
    </array>
  </dict>
</array>
```

---

该模板可在任意数量的使用 Firebase Hosting 的 MAUI 项目中复用 🔁
