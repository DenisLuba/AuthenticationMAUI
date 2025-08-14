# En

# Firebase Google Auth for .NET MAUI

## ✅ Overview

This template uses FirebaseAuthentication.net and WebAuthenticator. It provides:

* Firebase Hosting (`redirect.html `)
* and the 'AuthenticationMAUI` library, which connects Google Login in the MAUI application. It also implements authentication via Email in Firebase and via SMS by phone number (THIS IS A PAID SERVICE currently available in the Blaze tariff) with reCAPTCHA.

For an example of hosting on Firebase, see the "AuthenticationMAUI.FirebaseHostTemplate" folder
---

## Step by step

### 1. Creating a Firebase project

1. Go to [Firebase](https://console.firebase.google.com)
2. Create a project (for example, `myapp-auth`)
3. Enable Authentication > Sign-in method > Google (for authentication via Google)
4. Remember the values:
* Web API Key (**Project Settings > General > Web API Key**) (for authentication via Google)
* Auth domain (**Authentication > Settings > Authorized Domains**) — usually `project-id.firebaseapp.com `
5. Enable Authentication > Sign-in method > Phone (for authentication via Phone)

### 2. Creating an OAuth 2.0 Client ID for authentication via Google

1. Open [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Create, if not already created, an `OAuth 2.0 Client ID`:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Remember the `client_id' (in the same place or in the Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID)

### 3. Create a reCAPTCHA key for Phone authentication with reCAPTCHA

1. Open [Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha) and create key
2. Application Type - Web
3. Add a domain - (**Firebase Project > Authentication > Settings > Authorized Domains**) — usually `project-id.firebaseapp.com `
4. Next Step > Will you use challenges - Yes > Checkbox challenge
5. Create Key > Save the Site Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > ID of yours key) and Secret Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > Key details > (Continue with the instructions) Use legacy key)

OR

1. Click on the [link](https://www.google.com/recaptcha/admin/create)
2. Add some kind of label (it doesn't matter which one)
3. reCAPTCHA type: Using tasks (v2) - "I am not a robot" checkbox
4. Add a domain from Firebase (Authentication > Settings > Authorized Domains) — usually project-id.firebaseapp.com
5. Select the appropriate project
6. Click "Send"
7. Save the Site Key and Secret Key

### 4. Setting up firebase hosting

1. Install, if not installed, `firebase-tools` via the terminal [View → Terminal], located in the root directory of the project (first download and install [Node.js](https://nodejs.org/en/download/current)):

```bash
npm install -g firebase-tools
```

2. Enter:

```bash
firebase login
```

3. Initialize hosting (take the project name from Firebase):

```bash
firebase init hosting
```

4. Answer the questions from firebase:
```bash
1. Are you ready to proceed? Y
2. Please select an option:
- Add Firebase to an existring Google Cloud Platform project
3. Select the Google Cloud Platform project you would like to add Firebase: your project
4. What do you want to use your public directory? public
5. Configure as a single-page app(rewrite allurls to /index.html)? N
6. Set up authomatic builds and deploys with GitHub? N
```

### 5. Create a file redirect.html (for authentication via Google)

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

### 6. Change the firebase.json file (for authentication via Google)

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

### 7. Create a file recaptcha.html (for the Phone authentication with reCAPTCHA)

`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>reCAPTCHA</title>
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    <script>
        function onSubmit(token) {
            window.location.href = "recaptcha://token?" + encodeURIComponent(token);
        }
    </script>
</head>
<body>
    <h3>Checking reCAPTCHA</h3>
<form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```
Replace "**__YOUR_SITE_KEY__**" with the public key (site key) from step 3.7.

### 8. Deployment

```bash
firebase deploy --only hosting
```

---

### 9. 🔗 Adding to an existing MAUI project

1. Clone the repository:

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. In Visual Studio: PCM on solution → `Add > Existing Project...` → select `AuthenticationMAUI.csproj`
3. Then: PCM on the MAUI project → `Add > Project Reference...` → mark `AuthenticationMAUI`

---

### 10. 🌐 How to use FirebaseLoginService

1. Send FirebaseLoginData via DI to MauiProgram.cs:

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new ()
        {
            UserStorageService = userStorageService,
            ApiKey = apiKey, // Your Web API Key from the Firebase Console (Firebase Console > Project Settings > General > "Web API Key")
AuthDomain = authDomain, // Usually this your-project-id.firebaseapp.com (Firebase Console > Authentication > Settings > "Authorized domains")
GoogleClientId = googleClientId, // Your Google Client ID (Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID")
            GoogleRedirectUri = googleRedirectUri, // Usually it is "https://your-project-id.firebaseapp.com/__/auth/handler ", but "__/auth/handler" is changed to "redirect.html ",
// to make it work "https://your-project-id.firebaseapp.com/redirect.html "
                                                   // (Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs)
            CallbackScheme = callbackScheme, // A callback scheme for authentication via Google. For example, "myapp" for myapp:// (but you can also use myapp:// - this will be edited in the constructor)
            SecretKey = secretKey // Your Secret Key for reCAPTCHA from step 3.7
});
});
```

2. To authenticate via Google, add the intent-filter for Android to `MainActivity.cs`, for example, you can add it below the MainActivity class in the same file:

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Android.Content.Intent.ActionView],
    Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
private const string CALLBACK_SCHEME = "myapp"; // Must match the callback scheme of CallbackScheme (passed to FirebaseLoginService)
}
```

3. Add to Info.plist (for iOS):

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

Successfully! Now this template can be reused in hundreds of MAUI projects with Firebase Hosting!🔁

# Fr (Traduit par ai)

# Authentification Google Firebase pour .NET MAUI

## ✅ Aperçu

Ce modèle utilise FirebaseAuthentication.net et WebAuthenticator. Il fournit :

* Firebase Hosting (`redirect.html`)
* et la bibliothèque 'AuthenticationMAUI', qui connecte Google Login dans l'application MAUI. Il implémente également l'authentification par e-mail dans Firebase et par SMS par numéro de téléphone (IL S'AGIT d'UN SERVICE PAYANT actuellement disponible dans le tarif Blaze) avec reCAPTCHA.

Pour un exemple d'hébergement sur Firebase, voir le dossier "AuthenticationMAUI.FirebaseHostTemplate"
---

## Configuration étape par étape

### 1. Création d'un projet Firebase

1. Aller à [Firebase](https://console.firebase.google.com)
2. Créez un projet (par exemple, `myapp-auth`)
3. Activer Authentication > Sign-in method > Google (pour l'authentification via Google)
4. Rappelez-vous les valeurs:
* Web API Key (**Project Settings > General > Web API Key**) (pour l'authentification via Google)
* Auth domain (**Authentication > Settings > Authorized Domains**) - généralement `project-id.firebaseapp.com `
5. Activer Authentication > Sign-in method > Phone (pour l'authentification par Téléphone)

### 2. Création d'un ID client OAuth 2.0 pour l'authentification via Google

1. Ouvrez [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Si vous n’en avez pas encore créé, créez un `identifiant client OAuth 2.0` :

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Copiez votre `client_id` (au même endroit ou dans Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID)

### 3. Créer une clé reCAPTCHA pour l'authentification par Téléphone avec reCAPTCHA

1. Ouvrez [Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha) et créer une clé
2. Application Type - Web
3. Add a domain - (**Firebase Project > Authentication > Settings > Authorized Domains**) - généralement `project-id.firebaseapp.com`
4. Next Step > Will you use challenges - Yes > Checkbox challenge
5. Create Key > Save the Site Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > ID of yours key) et Secret Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > Key details > (Continue with the instructions) Use legacy key)

OU

1. Cliquez sur le [lien](https://www.google.com/recaptcha/admin/create)
2. Ajoutez une sorte d'étiquette (peu importe laquelle)
3. reCAPTCHA type: Using tasks (v2) - "I am not a robot" checkbox
4. Ajouter un domaine à partir de Firebase (Authentication > Settings > Authorized Domains) - généralement project-id.firebaseapp.com
5. Sélectionnez le projet approprié
6. Cliquez sur "Send"
7. Enregistrez Site Key et Secret Key

### 4. Configurer Firebase Hosting

1. Si ce n’est pas déjà fait, installez `firebase-tools` via le terminal [Affichage → Terminal], à la racine du projet (commencez par installer Node.js : https://nodejs.org/en/download/current) :

```bash
npm install -g firebase-tools
```

2. Connectez-vous :

```bash
firebase login
```

3. Initialisez l’hébergement (utilisez l’ID de votre projet) :

```bash
firebase init hosting
```

4. Répondez aux questions de firebase :
```bash
1. Êtes-vous prêt à continuer ? Y
2. Veuillez sélectionner une option :
- Ajouter Firebase à un projet Google Cloud Platform existant
3. Sélectionnez le projet GCP auquel vous souhaitez ajouter Firebase : votre projet
4. Quel répertoire public souhaitez-vous utiliser ? public
5. Configurer comme une application monopage (réécrire toutes les URL vers /index.html) ? N
6. Configurer des builds et déploiements automatiques avec GitHub ? N
```

### 5. Créer un fichier `redirect.html` (pour l'authentification via Google)

Dans `public/redirect.html` :

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

### 6. Modifier le fichier `firebase.json` (pour l'authentification via Google)

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

### 7. Créez un fichier recaptcha.html (pour l'authentification par SMS avec reCAPTCHA)
`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>reCAPTCHA</title>
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    <script>
        function onSubmit(token) {
            window.location.href = "recaptcha://token?" + encodeURIComponent(token);
        }
    </script>
</head>
<body>
    <h3>Checking reCAPTCHA</h3>
<form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```
Remplacez "**__YOUR_SITE_KEY__**" par la clé publique (site key) de l'étape 3.7.

### 8. Déployer

```bash
firebase deploy --only hosting
```

---

### 9. 🔗 Ajouter à votre projet MAUI

1. Clonez le dépôt :

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. Dans Visual Studio : Clic droit sur la solution → `Add > Existing Project...` → sélectionnez `AuthenticationMAUI.csproj`

3. Puis : clic droit sur votre projet MAUI → `Add > Project Reference...` → sélectionnez `AuthenticationMAUI`

---

### 10. 🌐 Utiliser `FirebaseLoginService`

1. Enregistrez `FirebaseLoginData` dans le conteneur DI :

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new ()
        {
            UserStorageService = userStorageService,
            ApiKey = apiKey, // Votre clé API Web depuis la console Firebase (Firebase Console > Paramètres du projet > Général > "Web API Key")
            AuthDomain = authDomain, // Généralement votre-project-id.firebaseapp.com (Firebase Console > Authentication > Paramètres > "Domaines autorisés")
            GoogleClientId = googleClientId, // Votre identifiant client Google (Firebase Console > Authentication > Méthode de connexion > Google > Configuration Web SDK > "Web client ID")
            GoogleRedirectUri = googleRedirectUri, // Généralement "https://your-project-id.firebaseapp.com/__/auth/handler", mais ici on le change en "redirect.html"
                                                   // Cela devient donc "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // (Google Cloud Console > APIs & Services > Credentials > OAuth 2.0 Client IDs > Client Web > URIs de redirection autorisés)
            CallbackScheme = callbackScheme, // Le schéma de rappel utilisé pour l’authentification Google. Par exemple, "myapp" pour myapp://
            SecretKey = SecretKey / / Votre clé secrète pour reCAPTCHA à partir de l'étape 3.7
        });
});
```

2. Ajouter un intent filter dans `MainActivity.cs` sous Android, par exemple juste après la classe MainActivity :

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Android.Content.Intent.ActionView],
    Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // Doit correspondre au Callback Scheme passé à FirebaseLoginService
}
```

3. Ajouter au fichier `Info.plist` (iOS) :

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

Ce modèle peut être réutilisé pour un nombre illimité de projets MAUI avec Firebase Hosting 🔁

# Ru

# Firebase Google Auth for .NET MAUI

## ✅ Обзор

Этот шаблон использует FirebaseAuthentication.net и WebAuthenticator. Он обеспечивает:

* Firebase Hosting (`redirect.html`)
* и библиотеку `AuthenticationMAUI`, которая подключает Google Login в MAUI-приложении. Также в ней реализована аутентификация через Email в Firebase и через СМС по номеру телефона (ЭТО ПЛАТНАЯ УСЛУГА, доступная на данный момент в тарифе Blaze) с прохождением reCAPTCHA.

Пример хостинга на Firebase смотри в папке "AuthenticationMAUI.FirebaseHostTemplate"
---

## Пошагово

### 1. Создание Firebase-проекта

1. Перейди в [Firebase](https://console.firebase.google.com)
2. Создай проект (например, `myapp-auth`)
3. Включи Authentication > Sign-in method > Google (для аутентификации через Google)
4. Запомни значения:
   * Web API Key (**Project Settings > General > Web API Key**) (для аутентификации через Google)
   * Auth domain (**Authentication > Settings > Authorized Domains**) — обычно `project-id.firebaseapp.com`
5. Включи Authentication > Sign-in method > Phone (для аутентификации через CMC)

### 2. Создание OAuth 2.0 Client ID для аутентификации через Google

1. Открой [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Создай, если еще не создан, `OAuth 2.0 Client ID`:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Запомни `client_id` (там же или в Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID)

### 3. Создай ключ reCAPTCHA для аутентификации по СМС с reCAPTCHA

1. Открой [Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha) и создай ключ
2. Application Type - Web
3. Add a domain - (**Firebase Project > Authentication > Settings > Authorized Domains**) — обычно `project-id.firebaseapp.com `
4. Next Step > Will you use challenges - Да > Checkbox challenge
5. Create Key > Save the Site Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > ID of yours key) и Secret Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > Key details > (Continue with the instructions) Use legacy key)

ИЛИ

1. Нажмите на [ссылку](https://www.google.com/recaptcha/admin/create)
2. Добавьте какой-нибудь ярлык (неважно, какой именно)
3. reCAPTCHA type: С помощью заданий (v2) - Флажок "I am not a robot" 
4. Добавьте домен из Firebase (Authentication > Settings > Authorized Domains) — обычно project-id.firebaseapp.com
5. Выберите подходящий проект
6. Нажмите "Отправить"
7. Сохраните ключ сайта и секретный ключ

### 4. Настрой firebase hosting

1. Установи, если не установлен, `firebase-tools` через терминал [View → Terminal], находясь в корневой директории проекта (вначале скачай и установи [Node.js](https://nodejs.org/en/download/current)):

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

### 5. Создай файл redirect.html (для аутентификации через Google)

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

### 6. Измени файл firebase.json (для аутентификации через Google)

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

### 7. Создай файл recaptcha.html (для аутентификации по СМС с reCAPTCHA)

`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>reCAPTCHA</title>
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    <script>
        function onSubmit(token) {
            window.location.href = "recaptcha://token?" + encodeURIComponent(token);
        }
    </script>
</head>
<body>
    <h3>Проверка reCAPTCHA</h3>
    <form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```
Замени "**__YOUR_SITE_KEY__**" на публичный ключ (site key) из шага 3.7.

### 8. Деплой

```bash
firebase deploy --only hosting
```

---

### 9. 🔗 Добавление в существующий MAUI проект

1. Клонируй репозиторий:

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. В Visual Studio: ПКМ на решении → `Add > Existing Project...` → выбери `AuthenticationMAUI.csproj`
3. Затем: ПКМ на проекте MAUI → `Add > Project Reference...` → отметь `AuthenticationMAUI`

---

### 10. 🌐 Как использовать FirebaseLoginService

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
            CallbackScheme = callbackScheme, // Схема обратного вызова для аутентификации через Google. Например, "myapp" для myapp:// (но можно и myapp:// - это будет отредактировано в конструкторе)
            SecretKey = secretKey // Ваш Secret Key для reCAPTCHA из шага 3.7
        });
});
```

2. Для аутентификации через Google добавь intent-filter для Android в `MainActivity.cs`, например, можешь добавить его ниже класса MainActivity в том же файле:

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

Успешно! Теперь этот шаблон можно переиспользовать в сотне проектов MAUI с Firebase Hosting!🔁

# Zh (AI翻译)
#Firebase Google Auth for.NET MAUI

## ✅ 概述

此模板使用FirebaseAuthentication.net 和WebAuthenticator。 它提供:

*Firebase托管（`redirect.html`)
*和'AuthenticationMAUI'库，它在毛伊岛应用程序中连接Google登录。 它还通过Firebase中的电子邮件和通过电话号码的短信（这是目前在Blaze资费中提供的付费服务）与reCAPTCHA实现身份验证。

有关在Firebase上托管的示例，请参阅AuthenticationMAUI.FirebaseHostTemplate文件夹
---

##循序渐进

### 1. 创建Firebase项目

1. 转到[Firebase](https://console.firebase.google.com)
2. 创建一个项目（例如，`myapp-auth`）
3. 启用Authentication > Sign-in method > Google（用于通过Google进行身份验证）
4. 记住价值观:
* Web API Key (Project Settings > General > Web API Key)（用于通过Google进行身份验证）
* Auth domain (Authentication > Settings > Authorized Domains) — 通常`project-id.firebaseapp.com `
5. 启用**身份验证>登录方法>电话**（用于电话身份验证）

### 2. 通过Google创建用于身份验证的OAuth2.0客户端ID

1. 打开[Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. 创建（如果尚未创建）`OAuth 2.0 Client ID`:
`OAuth 2.0 Client ID`:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. 记住'client_id'（在同一个地方或**Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID**）

### 3. 使用reCAPTCHA创建用于SMS身份验证的reCAPTCHA密钥

1. 打开[Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha) 并创建密钥
2. Application Type - Web
3. Add a domain - (Firebase Project > Authentication > Settings > Authorized Domains) -通常`project-id.firebaseapp.com`
4. Next Step > Will you use challenges - 是 > Checkbox challenge
5. Create Key > Save the Site Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > ID of yours key) 和 Secret Key ([reCAPTCHA](https://console.cloud.google.com/security/recaptcha) > reCAPTCHA Keys > Key details > (Continue with the instructions) Use legacy key)

或

1. 点击[ссылку](https://www.google.com/recaptcha/admin/create)
2. 添加某种标签（哪一个并不重要）
3. reCAPTCHA类型：使用作业（v2）-复选框"我不是机器人" 
4. 从Firebase添加域(Authentication > Settings > Authorized Domains) -通常project-id.firebaseapp.com
5. 选择合适的项目
6. 点击"发送"
7. 保存网站密钥和密钥

### 4. 设置 Firebase Hosting
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

### 5. 创建文件 `redirect.html`（用于通过Google进行身份验证）

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

### 6. 编辑文件 firebase.json （用于通过Google进行身份验证）

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

### 7. 创建一个文件 recaptch.html（用于使用reCAPTCHA进行电话身份验证）
`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>reCAPTCHA</title>
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    <script>
        function onSubmit(token) {
            window.location.href = "recaptcha://token?" + encodeURIComponent(token);
        }
    </script>
</head>
<body>
    <h3>Checking reCAPTCHA</h3>
<form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```
将"**__YOUR_SITE_KEY__**"替换为步骤3.7中的公钥（site key）

### 8.部署

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
            CallbackScheme = callbackScheme, // Google 登录回调的 scheme。例如 "myapp" 对应 myapp://（可以自定义）
            SecretKey = secretKey // 步骤3.7中的reCAPTCHA密钥
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


