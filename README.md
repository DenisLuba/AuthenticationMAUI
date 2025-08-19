# En

# Firebase Google Auth for .NET MAUI

## ✅ Overview

This template uses **FirebaseAuthentication.net** and **WebAuthenticator**. It provides:

* Firebase Hosting (`redirect.html`)
* and the `AuthenticationMAUI` library, which integrates **Google Login** into a MAUI application.
  It also includes authentication via **Email in Firebase** and **Phone Number with SMS** (⚠️ this is a **paid feature**, available only on the **Blaze plan**) using **reCAPTCHA**.

For an example Firebase hosting setup, see the folder: `AuthenticationMAUI.FirebaseHostTemplate`.

---

## Step-by-step guide

### 1. Create a Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com)
2. Create a new project (e.g., `myapp-auth`)
3. Enable **Authentication > Sign-in method > Google** (for Google authentication)
4. Remember these values:

   * **Web API Key** (`Project Settings > General > Web API Key`)
   * **Auth domain** (`Authentication > Settings > Authorized Domains`) — usually `project-id.firebaseapp.com`
5. Enable **Authentication > Sign-in method > Phone** (for SMS authentication)
6. Enable **Authentication > Sign-in method > Facebook** (for Facebook authentication)

---

### 2. Create OAuth 2.0 Client ID for Google authentication

1. Open [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Create (if not already created) an **OAuth 2.0 Client ID**:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Save the `client_id` (you can also find it in **Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID**)

---

### 3. Create a Facebook App in Meta for Developers

1. [Create a new app in Meta for Developers](https://developers.facebook.com/apps/creation/)
2. Configure it for Facebook Login
3. In the created app, go to:
   **Dashboard > Set up "Facebook Login" product > Settings**
4. In **"Valid OAuth Redirect URIs"** add:
   `https://project-id.firebaseapp.com/redirect.html`
5. In **"Allowed Domains for the JavaScript SDK"** add the Firebase domain (from **Authentication > Settings > Authorized Domains**) — usually `project-id.firebaseapp.com`

---

### 4. Create a reCAPTCHA key for Phone SMS authentication

1. Open [Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha) and create a new key
2. **Application Type**: Web
3. **Add a domain**: your Firebase authorized domain (e.g. `project-id.firebaseapp.com`)
4. **Use challenge**: Yes → Checkbox challenge
5. Create Key → Save the **Site Key** (public) and **Secret Key** (private)
   *Site key will be used in `recaptcha.html`, Secret key will be stored in `FirebaseLoginData.SecretKey`*

**Alternative way:**

1. Open [reCAPTCHA create link](https://www.google.com/recaptcha/admin/create)
2. Add any label (name doesn’t matter)
3. reCAPTCHA type: "Checkbox challenge" (I am not a robot)
4. Add your Firebase domain (e.g., `project-id.firebaseapp.com`)
5. Select your Firebase project
6. Save site key and secret key

---

### 5. Setup Firebase Hosting

1. Install `firebase-tools` (requires [Node.js](https://nodejs.org/en/download/current)):

```bash
npm install -g firebase-tools
```

2. Login:

```bash
firebase login
```

3. Initialize hosting:

```bash
firebase init hosting
```

4. Answer questions:

```
1. Are you ready to proceed? Y
2. Please select an option:
   - Add Firebase to an existing Google Cloud Platform project
3. Select your Firebase project
4. What do you want to use as your public directory? public
5. Configure as a single-page app? N
6. Set up automatic builds and deploys with GitHub? N
```

---

### 6. Create `redirect.html` (for Google and Facebook authentication)

`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Redirecting...</title>
</head>
<body>
    <h1>REDIRECTING...</h1>
    <pre id="output"></pre>
    <script>      
        const fragment = window.location.hash.substring(1); 
        const params = new URLSearchParams(fragment);

        const idToken = params.get('id_token');
        const accessToken = params.get('access_token');

        const scheme = params.get('state') || 'myapp';

        if (idToken) {
            // Google
            window.location.href = scheme + '://auth?id_token=' + idToken;
        } else if (accessToken) {
            // Facebook
            window.location.href = scheme + '://auth?access_token=' + accessToken;
        } else {
            document.body.innerHTML = '<h2>Token not found</h2>';
        }
    </script>
</body>
</html>
```

---

### 7. Update `firebase.json`

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

---

### 8. Create `recaptcha.html` (for SMS reCAPTCHA verification)

`public/recaptcha.html`:

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
    <h3>reCAPTCHA Verification</h3>
    <form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```

Replace `__YOUR_SITE_KEY__` with the **public site key** from step 4.

---

### 9. Deploy

```bash
firebase deploy --only hosting
```

---

### 10. 🔗 Add into existing MAUI project

1. Clone repository:

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. In Visual Studio:
   Solution → `Add > Existing Project...` → select `AuthenticationMAUI.csproj`
3. Then: Right-click on your MAUI project → `Add > Project Reference...` → check `AuthenticationMAUI`

---

### 11. 🌐 How to use `FirebaseLoginService`

1. Pass `FirebaseLoginData` via DI in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new()
        {
            UserStorageService = userStorageService,
            ApiKey = GlobalValues.API_KEY, // Your Web API Key from the Firebase Console (Firebase Console > Project Settings > General > "Web API Key")
            AuthDomain = GlobalValues.AUTH_DOMAIN, // Usually this your-project-id.firebaseapp.com (Firebase Console > Authentication > Settings > "Authorized domains")
            GoogleClientId = GlobalValues.GOOGLE_CLIENT_ID, // Your Google Client ID (Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID")
            GoogleRedirectUri = GlobalValues.REDIRECT_URI, // Usually it is "https://your-project-id.firebaseapp.com/__/auth/handler ", but "__/auth/handler" is changed to "redirect.html ",
                                                           // to make it work "https://your-project-id.firebaseapp.com/redirect.html "
                                                           // (Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs)
            CallbackScheme = GlobalValues.CALLBACK_SCHEME, // A callback scheme for authentication via Google. For example, "myapp" for myapp:// (but you can also use myapp:// - this will be edited in the constructor)
            SecretKey = secretKey // Your Secret Key for reCAPTCHA from step 3.7
            SecretKey = GlobalValues.SECRET_KEY, // the secret key that Google issues when registering reCAPTCHA (used only on the server to verify the token)
            FacebookAppId = GlobalValues.FACEBOOK_APP_ID, // Your Facebook App ID (Facebook for Developers > My Apps > [Your App] > Settings > Basic > App ID)
            FacebookRedirectUri = GlobalValues.REDIRECT_URI // Usually it is "https://your-project-id.firebaseapp.com/__/auth/handler ", but "__/auth/handler" is changed to "redirect.html ",
                                                            // to make it work "https://your-project-id.firebaseapp.com/redirect.html "
                                                            // (Meta for Developers > Panel > Set up the "Authentication and Data request from users using Facebook Login > settings > Valid redirect URIs for OAuth" scenario)
        });
});
```

2. For Android, add `intent-filter` in `MainActivity.cs`:

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // Must match the callback scheme of CallbackScheme (passed to FirebaseLoginService)
}
```

3. For iOS, add to `Info.plist`:

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

Replace `myapp` with the same callback scheme you pass to `FirebaseLoginService`.

---

🎉 Done!
Now you can reuse this template across multiple MAUI projects with Firebase Hosting! 🔁

---

# Fr (Traduit par AI)

# Authentification Google Firebase pour .NET MAUI

## ✅ Aperçu

Ce template utilise **FirebaseAuthentication.net** et **WebAuthenticator**. Il fournit :

* Firebase Hosting (`redirect.html`)
* et la librairie `AuthenticationMAUI`, qui intègre la connexion **Google Login** dans une application MAUI.
  Il inclut également l’authentification via **Email dans Firebase** et **Numéro de téléphone avec SMS** (⚠️ c’est une **fonctionnalité payante**, disponible uniquement avec le plan **Blaze**) avec **reCAPTCHA**.

Pour un exemple d’hébergement Firebase, voir le dossier : `AuthenticationMAUI.FirebaseHostTemplate`.

---

## Guide étape par étape

### 1. Créer un projet Firebase

1. Aller sur [Firebase Console](https://console.firebase.google.com)
2. Créer un projet (par ex. `myapp-auth`)
3. Activer **Authentication > Sign-in method > Google** (pour l’authentification Google)
4. Noter les valeurs suivantes :

   * **Web API Key** (`Project Settings > General > Web API Key`)
   * **Auth domain** (`Authentication > Settings > Authorized Domains`) — en général `project-id.firebaseapp.com`
5. Activer **Authentication > Sign-in method > Phone** (pour l’authentification par SMS)
6. Activer **Authentication > Sign-in method > Facebook** (pour l’authentification Facebook)

---

### 2. Créer un OAuth 2.0 Client ID pour Google

1. Ouvrir [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Créer (si pas déjà créé) un **OAuth 2.0 Client ID** :

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. Enregistrer le `client_id` (disponible aussi dans **Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID**)

---

### 3. Créer une application Facebook dans Meta for Developers

1. [Créer une nouvelle app dans Meta for Developers](https://developers.facebook.com/apps/creation/)
2. Configurer l’app pour Facebook Login
3. Dans l’app créée, aller dans :
   **Dashboard > Set up "Facebook Login" product > Settings**
4. Dans **"Valid OAuth Redirect URIs"**, ajouter :
   `https://project-id.firebaseapp.com/redirect.html`
5. Dans **"Allowed Domains for the JavaScript SDK"**, ajouter le domaine Firebase (depuis **Authentication > Settings > Authorized Domains**) — en général `project-id.firebaseapp.com`

---

### 4. Créer une clé reCAPTCHA pour l’authentification SMS

1. Ouvrir [Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha) et créer une clé
2. **Application Type** : Web
3. **Add a domain** : domaine autorisé Firebase (ex. `project-id.firebaseapp.com`)
4. **Use challenge** : Oui → Checkbox challenge
5. Créer la clé → Enregistrer la **Site Key** (publique) et la **Secret Key** (privée)
   *La Site Key sera utilisée dans `recaptcha.html`, la Secret Key sera stockée dans `FirebaseLoginData.SecretKey`*

**Alternative** :

1. Ouvrir [ce lien](https://www.google.com/recaptcha/admin/create)
2. Donner un label (n’importe lequel)
3. reCAPTCHA type : "Checkbox challenge" (I am not a robot)
4. Ajouter le domaine Firebase (ex. `project-id.firebaseapp.com`)
5. Sélectionner le projet
6. Enregistrer la site key et la secret key

---

### 5. Configurer Firebase Hosting

1. Installer `firebase-tools` (nécessite [Node.js](https://nodejs.org/en/download/current)) :

```bash
npm install -g firebase-tools
```

2. Se connecter :

```bash
firebase login
```

3. Initialiser l’hébergement :

```bash
firebase init hosting
```

4. Répondre aux questions :

```
1. Are you ready to proceed? Y
2. Please select an option:
   - Add Firebase to an existing Google Cloud Platform project
3. Select your Firebase project
4. What do you want to use as your public directory? public
5. Configure as a single-page app? N
6. Set up automatic builds and deploys with GitHub? N
```

---

### 6. Créer `redirect.html` (pour Google et Facebook)

`public/redirect.html` :

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Redirection...</title>
</head>
<body>
    <h1>REDIRECTION...</h1>
    <pre id="output"></pre>
    <script>      
        const fragment = window.location.hash.substring(1); // dans l’URL après '#'
        const params = new URLSearchParams(fragment);

        const idToken = params.get('id_token');
        const accessToken = params.get('access_token');

        const scheme = params.get('state') || 'myapp';

        if (idToken) {
            // Connexion Google
            window.location.href = scheme + '://auth?id_token=' + idToken;
        } else if (accessToken) {
            // Connexion Facebook
            window.location.href = scheme + '://auth?access_token=' + accessToken;
        } else {
            document.body.innerHTML = '<h2>Aucun token trouvé</h2>';
        }
    </script>
</body>
</html>
```

---

### 7. Modifier `firebase.json`

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

---

### 8. Créer `recaptcha.html` (pour SMS reCAPTCHA)

`public/recaptcha.html` :

```html
<!DOCTYPE html>
<html>
<head>
    <title>reCAPTCHA</title>
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    <script>
        function onSubmit(token) {
            // On redirige le token vers l’application MAUI
            window.location.href = "recaptcha://token?" + encodeURIComponent(token);
        }
    </script>
</head>
<body>
    <h3>Vérification reCAPTCHA</h3>
    <form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```

Remplacer `__YOUR_SITE_KEY__` par la **clé publique** du reCAPTCHA.

---

### 9. Déploiement

```bash
firebase deploy --only hosting
```

---

### 10. 🔗 Ajouter dans un projet MAUI existant

1. Cloner le dépôt :

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. Dans Visual Studio :
   Solution → `Add > Existing Project...` → sélectionner `AuthenticationMAUI.csproj`
3. Puis : clic droit sur votre projet MAUI → `Add > Project Reference...` → cocher `AuthenticationMAUI`

---

### 11. 🌐 Utiliser `FirebaseLoginService`

1. Passer `FirebaseLoginData` via DI dans `MauiProgram.cs` :

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new()
        {
            UserStorageService = userStorageService,
            ApiKey = GlobalValues.API_KEY, // Votre clé API Web depuis la console Firebase (Firebase Console > Paramètres du projet > Général > "Web API Key")
            AuthDomain = GlobalValues.AUTH_DOMAIN, // Généralement votre-project-id.firebaseapp.com (Firebase Console > Authentication > Paramètres > "Domaines autorisés")
            GoogleClientId = GlobalValues.GOOGLE_CLIENT_ID, // Votre identifiant client Google (Firebase Console > Authentication > Méthode de connexion > Google > Configuration Web SDK > "Web client ID")
            GoogleRedirectUri = GlobalValues.REDIRECT_URI, // Généralement "https://your-project-id.firebaseapp.com/__/auth/handler", mais ici on le change en "redirect.html"
                                                   // Cela devient donc "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // (Google Cloud Console > APIs & Services > Credentials > OAuth 2.0 Client IDs > Client Web > URIs de redirection autorisés)
            CallbackScheme = GlobalValues.CALLBACK_SCHEME, // Le schéma de rappel utilisé pour l’authentification Google. Par exemple, "myapp" pour myapp://
            SecretKey = GlobalValues.SECRET_KEY, // Votre clé secrète pour reCAPTCHA à partir de l'étape 3.7
            FacebookAppId = GlobalValues.FACEBOOK_APP_ID, // ID d’application Facebook (Facebook for Developers > My Apps > [Your App] > Settings > Basic > App ID)
            FacebookRedirectUri = GlobalValues.REDIRECT_URI // Généralement "https://your-project-id.firebaseapp.com/__/auth/handler", mais ici on le change en "redirect.html"
                                                   // Cela devient donc "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // (Meta for Developers > Panel > Set up the "Authentication and Data request from users using Facebook Login > settings > Valid redirect URIs for OAuth" scenario)

        });
});
```

2. Pour Android, ajouter un `intent-filter` dans `MainActivity.cs` :

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // doit correspondre à CallbackScheme passé au service
}
```

3. Pour iOS, ajouter dans `Info.plist` :

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

Remplacer `myapp` par le schéma que vous avez configuré dans `FirebaseLoginService`.

---

🎉 Fini !
Ce template peut maintenant être réutilisé dans plusieurs projets MAUI avec Firebase Hosting 🔁

---

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
6. Включи Authentication > Sign-in method > Facebook (для аутентификации через Facebook)

### 2. Создание OAuth 2.0 Client ID для аутентификации через Google

1. Открой [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. Создай, если еще не создан, `OAuth 2.0 Client ID`:

   * Type: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html` (соответствующее файлу redirect на хостинге Firebase)
3. Запомни `client_id` (там же или в Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID)

### 3. Создание Facebook App в Meta for Developers

1. [Создай новое приложение в Meta for Developers](https://developers.facebook.com/apps/creation/)
2. Настрой его на аутентификацию через facebook
3. В созданном приложении зайди в **Панель > Настройте сценарий "Аутентификация и запрос данных у пользователей с помощью функции "Вход через Facebook" > настройки**
4. В **"Действительные URI перенаправления для OAuth"** добавьте URL типа `https://project-id.firebaseapp.com/redirect.html` (соответствующее файлу redirect на хостинге Firebase)
5. В **Разрешенные домены для SDK JavaScript** добавьте домен из Firebase (**Authentication > Settings > Authorized Domains**) — обычно `project-id.firebaseapp.com`

### 4. Создай ключ reCAPTCHA для аутентификации по СМС с reCAPTCHA

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

### 5. Настрой firebase hosting

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

### 6. Создай файл redirect.html (для аутентификации через Google и через Facebook)

`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Redirecting...</title>
</head>
<body>
    <h1>REDIRECTING...</h1>
    <pre id="output"></pre>
    <script>      
        const fragment = window.location.hash.substring(1); // в url после '#'
        const params = new URLSearchParams(fragment);

        const idToken = params.get('id_token');
        const accessToken = params.get('access_token');

        const scheme = params.get('state') || 'myapp';

        if (idToken) {
            // Google
            window.location.href = scheme + '://auth?id_token=' + idToken;
        } else if (accessToken) {
            // Facebook
            window.location.href = scheme + '://auth?access_token=' + accessToken;
        } else {
            document.body.innerHTML = '<h2>Token not found</h2>';
        }
    </script>
</body>
</html>
```

### 7. Измени файл firebase.json (для аутентификации через Google и через Facebook)

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

### 8. Создай файл recaptcha.html (для аутентификации по СМС с reCAPTCHA)

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

### 9. Деплой

```bash
firebase deploy --only hosting
```

---

### 10. 🔗 Добавление в существующий MAUI проект

1. Клонируй репозиторий:

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. В Visual Studio: ПКМ на решении → `Add > Existing Project...` → выбери `AuthenticationMAUI.csproj`
3. Затем: ПКМ на проекте MAUI → `Add > Project Reference...` → отметь `AuthenticationMAUI`

---

### 11. 🌐 Как использовать FirebaseLoginService

1. Передай FirebaseLoginData через DI в MauiProgram.cs:

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
            return new FirebaseLoginService(
                new()
                {
                    UserStorageService = userStorageService,
                    ApiKey = GlobalValues.API_KEY, // Ваш Web API Key из Firebase Console (Firebase Console > Project Settings > General > "Web API Key")
                    AuthDomain = GlobalValues.AUTH_DOMAIN, // Обычно это your-project-id.firebaseapp.com (Firebase Console > Authentication > Settings > "Authorized domains")
                    GoogleClientId = GlobalValues.GOOGLE_CLIENT_ID, // Ваш Google Client ID (Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID")
                    GoogleRedirectUri = GlobalValues.REDIRECT_URI, // Обычно в Google Cloud Console изначально это "https://your-project-id.firebaseapp.com/__/auth/handler",
                                                                   // но "__/auth/handler" меняем на "redirect.html",
                                                                   // чтобы получилось "https://your-project-id.firebaseapp.com/redirect.html"
                                                                   // (Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs)
                    CallbackScheme = GlobalValues.CALLBACK_SCHEME, // Схема обратного вызова для аутентификации через Google.
                                                                   // Например, "myapp" для myapp:// (но можно и myapp:// - это будет отредактировано в конструкторе класса FirebaseLoginService).
                                                                   // Можно использовать "your_project_id" - имя вашего проекта
                    SecretKey = GlobalValues.SECRET_KEY, // секретный ключ, который выдает Google при регистрации reCAPTCHA (используется только на сервере для проверки токена).
                    FacebookAppId = GlobalValues.FACEBOOK_APP_ID, // Ваш Facebook App ID (Facebook for Developers > My Apps > [Your App] > Settings > Basic > App ID)
                    FacebookRedirectUri = GlobalValues.REDIRECT_URI // Обычно в Google Cloud Console изначально это "https://your-project-id.firebaseapp.com/__/auth/handler",
                                                                    // но "__/auth/handler" меняем на "redirect.html",
                                                                    // чтобы получилось "https://your-project-id.firebaseapp.com/redirect.html"
                                                                    // (Meta for Developers > Панель > Настройте сценарий "Аутентификация и запрос данных у пользователей с помощью функции "Вход через Facebook" > настройки > Действительные URI перенаправления для OAuth)
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

Вместо myapp вставь ту схему, которая указана в качестве схемы обратного вызова CallbackScheme (переданной в FirebaseLoginService)

---

Успешно! Теперь этот шаблон можно переиспользовать в сотне проектов MAUI с Firebase Hosting!🔁

Отлично 🚀
Теперь переведу README на **китайский (简体中文)**, сохраняя структуру и контекст.
Комментарии в коде тоже переведены, но все пути внутри **Google Console / Firebase / Meta** я оставляю на **英文**, как и раньше.

---

# Zh-CN （AI翻译）

# Firebase Google 身份验证 for .NET MAUI

## ✅ 概述

该模板使用 **FirebaseAuthentication.net** 和 **WebAuthenticator**。它提供：

* Firebase Hosting (`redirect.html`)
* `AuthenticationMAUI` 库，用于在 MAUI 应用中接入 **Google 登录**。
  同时还实现了 **Firebase 邮箱登录** 和 **手机号短信登录**（⚠️ 短信登录是 **付费功能**，目前只在 **Blaze 套餐**中可用），并配合 **reCAPTCHA**。

Firebase Hosting 示例见文件夹 `AuthenticationMAUI.FirebaseHostTemplate`。

---

## 步骤

### 1. 创建 Firebase 项目

1. 打开 [Firebase Console](https://console.firebase.google.com)
2. 创建一个项目 (例如 `myapp-auth`)
3. 启用 **Authentication > Sign-in method > Google**（Google 登录）
4. 记录以下值：

   * **Web API Key** (`Project Settings > General > Web API Key`)
   * **Auth domain** (`Authentication > Settings > Authorized Domains`) — 通常是 `project-id.firebaseapp.com`
5. 启用 **Authentication > Sign-in method > Phone**（手机号短信登录）
6. 启用 **Authentication > Sign-in method > Facebook**（Facebook 登录）

---

### 2. 创建 Google OAuth 2.0 Client ID

1. 打开 [Google Cloud Console > API & Services > Credentials](https://console.cloud.google.com/apis/credentials)
2. 如果还没有，创建一个 **OAuth 2.0 Client ID**：

   * 类型: Web Application
   * Authorized redirect URIs: `https://project-id.firebaseapp.com/redirect.html`
3. 保存 `client_id`（也可在 Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > Web client ID 找到）

---

### 3. 创建 Facebook 应用 (Meta for Developers)

1. [在 Meta for Developers 创建一个新应用](https://developers.facebook.com/apps/creation/)
2. 配置应用以支持 Facebook 登录
3. 在应用中进入：
   **Dashboard > Set up "Facebook Login" product > Settings**
4. 在 **"Valid OAuth Redirect URIs"** 中添加：
   `https://project-id.firebaseapp.com/redirect.html`
5. 在 **"Allowed Domains for the JavaScript SDK"** 中添加 Firebase 域名 (Firebase Console > Authentication > Settings > Authorized Domains) — 通常是 `project-id.firebaseapp.com`

---

### 4. 创建 reCAPTCHA 密钥（用于短信认证）

1. 打开 [Google Cloud Console > Security > reCAPTCHA](https://console.cloud.google.com/security/recaptcha)，创建一个密钥
2. **Application Type**: Web
3. **Add a domain**: 添加 Firebase 授权域名 (例如 `project-id.firebaseapp.com`)
4. **Use challenge**: 是 → Checkbox challenge
5. 创建密钥后，保存 **Site Key**（公钥） 和 **Secret Key**（私钥）
   *Site Key 用于 `recaptcha.html`，Secret Key 存放在 `FirebaseLoginData.SecretKey`*

**或者**：

1. 打开 [reCAPTCHA 创建页面](https://www.google.com/recaptcha/admin/create)
2. 输入一个标签（随意）
3. reCAPTCHA 类型: "Checkbox challenge (v2)"
4. 添加 Firebase 域名 (例如 `project-id.firebaseapp.com`)
5. 选择项目
6. 保存 **Site Key** 和 **Secret Key**

---

### 5. 配置 Firebase Hosting

1. 安装 `firebase-tools` (需要先安装 [Node.js](https://nodejs.org/en/download/current))：

```bash
npm install -g firebase-tools
```

2. 登录 Firebase：

```bash
firebase login
```

3. 初始化 Hosting：

```bash
firebase init hosting
```

4. 回答 Firebase 的问题：

```
1. Are you ready to proceed? Y
2. Please select an option:
   - Add Firebase to an existing Google Cloud Platform project
3. Select your Firebase project
4. What do you want to use as your public directory? public
5. Configure as a single-page app? N
6. Set up automatic builds and deploys with GitHub? N
```

---

### 6. 创建 `redirect.html` (用于 Google 和 Facebook 登录)

`public/redirect.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>正在重定向...</title>
</head>
<body>
    <h1>REDIRECTING...</h1>
    <pre id="output"></pre>
    <script>      
        const fragment = window.location.hash.substring(1); // URL 中 '#' 后面的部分
        const params = new URLSearchParams(fragment);

        const idToken = params.get('id_token');
        const accessToken = params.get('access_token');

        const scheme = params.get('state') || 'myapp';

        if (idToken) {
            // Google 登录
            window.location.href = scheme + '://auth?id_token=' + idToken;
        } else if (accessToken) {
            // Facebook 登录
            window.location.href = scheme + '://auth?access_token=' + accessToken;
        } else {
            document.body.innerHTML = '<h2>未找到 Token</h2>';
        }
    </script>
</body>
</html>
```

---

### 7. 修改 `firebase.json`

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

---

### 8. 创建 `recaptcha.html` (用于短信验证)

`public/recaptcha.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>reCAPTCHA</title>
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    <script>
        function onSubmit(token) {
            // 将 reCAPTCHA token 返回到 MAUI 应用
            window.location.href = "recaptcha://token?" + encodeURIComponent(token);
        }
    </script>
</head>
<body>
    <h3>reCAPTCHA 验证</h3>
    <form action="?" method="POST">
        <div class="g-recaptcha"
             data-sitekey="**__YOUR_SITE_KEY__**"
             data-callback="onSubmit">
        </div>
    </form>
</body>
</html>
```

请将 `__YOUR_SITE_KEY__` 替换为步骤 4 中生成的 **Site Key**（公钥）。

---

### 9. 部署

```bash
firebase deploy --only hosting
```

---

### 10. 🔗 在现有 MAUI 项目中使用

1. 克隆仓库：

```bash
git clone https://github.com/DenisLuba/AuthenticationMAUI.git
```

2. 在 Visual Studio 中：
   右键解决方案 → `Add > Existing Project...` → 选择 `AuthenticationMAUI.csproj`
3. 然后：右键 MAUI 项目 → `Add > Project Reference...` → 勾选 `AuthenticationMAUI`

---

### 11. 🌐 使用 `FirebaseLoginService`

1. 在 `MauiProgram.cs` 中通过依赖注入传递 `FirebaseLoginData`：

```csharp
builder.Services.AddSingleton<IUserStorageService, UserSecureStorageService>();
builder.Services.AddSingleton<ILoginService>(provider =>
{
    var userStorageService = provider.GetRequiredService<IUserStorageService>();
    return new FirebaseLoginService(
        new()
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

            FacebookAppId = GlobalValues.FACEBOOK_APP_ID, // Facebook 应用 ID (Facebook for Developers > My Apps > [Your App] > Settings > Basic > App ID)

            FacebookRedirectUri = GlobalValues.REDIRECT_URI // 通常为 "https://your-project-id.firebaseapp.com/__/auth/handler"，但我们将 "__/auth/handler" 替换为 "redirect.html"，即
                                                   // "https://your-project-id.firebaseapp.com/redirect.html"
                                                   // // (Meta for Developers > Panel > Set up the "Authentication and Data request from users using Facebook Login > settings > Valid redirect URIs for OAuth" scenario)
        });
});
```

2. Android 中，在 `MainActivity.cs` 添加 `intent-filter`：

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "myapp"; // 必须与 FirebaseLoginService 中传入的 CallbackScheme 保持一致
}
```

3. iOS 中，在 `Info.plist` 添加：

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

将 `myapp` 替换为你在 `FirebaseLoginService` 中配置的 CallbackScheme。

---

🎉 完成！
现在该模板可以在多个 MAUI 项目中重复使用，并支持 Firebase Hosting 🔁

---
