# En

# Firebase Google Auth for .NET MAUI

## ✅ Overview

This template includes:

* Firebase Hosting (`redirect.html`)
* `AuthenticationMAUI` library for Google Login in .NET MAUI apps. It also implements Email authentication in Firebase

---

## Step-by-step Setup

### 1. Create a Firebase Project

1. Go to [https://console.firebase.google.com](https://console.firebase.google.com)
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

1. Install Firebase CLI:

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

4. Set `public` as your public directory

### 4. Create `redirect.html`

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

### 5. Configure `firebase.json`

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
git clone https://github.com/your-org/AuthenticationMAUI.git
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
    private const string CALLBACK_SCHEME = "todolist"; // Must match the Callback Scheme (passed to FirebaseLoginService)
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

## ✅ Test

1. Open:

```bash
https://project-id.firebaseapp.com/redirect.html?scheme=myapp
```

2. Google redirects to `myapp://auth?id_token=...`
3. Your MAUI app captures `id_token` and logs in successfully

---

This template is reusable for any number of MAUI projects with Firebase Hosting 🔁

# Ru

# Firebase Google Auth for .NET MAUI

## ✅ Обзор

Этот шаблон обеспечивает:

* Firebase Hosting (`redirect.html`)
* и библиотеку `AuthenticationMAUI`, которая подключает Google Login в MAUI-приложении. Также в ней реализована аутентификация через Email в Firebase

---

## Пошагово

### 1. Создание Firebase-проекта

1. Перейди в [https://console.firebase.google.com](https://console.firebase.google.com)
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

1. Установи `firebase-tools` ([https://nodejs.org/en/download/current](https://nodejs.org/en/download/current)):

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

4. Укажи `public` как папку

### 4. Файл redirect.html

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

### 5. Файл firebase.json

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
git clone https://github.com/your-org/AuthenticationMAUI.git
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
    private const string CALLBACK_SCHEME = "todolist"; // Должно совпадать со схемой обратного вызова CallbackScheme (переданной в FirebaseLoginService)
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

## 📌 Тестовый запуск

* Проверь:

```bash
https://project-id.firebaseapp.com/redirect.html?scheme=myapp
```

* Google перенаправит на `myapp://auth?id_token=...`
* MAUI приложение получит `id_token` и войдёт

---

Успешно! Теперь этот шаблон можно переиспользовать в сотне проектов MAUI с Firebase Hosting!

