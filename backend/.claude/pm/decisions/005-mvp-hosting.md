# 005 · Деплой MVP на безкоштовний хостинг: Cloudflare Pages, Render, Neon

- **Статус:** accepted (2026-11-19).
  - **Вибір платформ, регіонів і назв** зробив власник 2026-11-19. Умова власника: безкоштовно і без банківської картки.
  - **Деталі реалізації** (розділи 3–7) склав PM. Власник може змінити їх до PRP-024.
- **Хто запропонував:** власник. Порівняння варіантів зібрала головна сесія 2026-11-18, PM перевірив факти 2026-11-19 (джерела в кінці).
- **Змінює:**
  - spec, розділ 6: пункт «деплой на prepround.app» стає пунктом «власний домен prepround.app»;
  - рішення 002: необов'язкове поле теми `draft` (розділ 6 нижче, «Уточнення» в 002);
  - рішення 003, п. 7: у Production `Cors:Origins` — `https://prepround.pages.dev`, а `http://localhost:4300` лише в Development.
- **Картки:** PRP-020 → PRP-021 → PRP-023 → PRP-022 → PRP-024.

## Контекст

Веб-MVP і контент Backend (42 теми, 504 питання, усі з PASS фактчеку) працюють лише на Маку власника. Щоб показати продукт людям, які готуються до співбесід, потрібна публічна адреса. Акаунтів немає, сервер нічого про користувача не зберігає, API лише читає каталог і питання. Тож вимоги до хостингу скромні:

- контейнер .NET 10;
- Postgres на кілька МБ;
- статика Angular.

Обидва репо вже на GitHub, гілка `master` (2026-11-19):

| Репо | GitHub | Локальна тека |
|---|---|---|
| бекенд і контент | `dmitryspain/Prepround` | `Prepround` |
| фронт | `dmitryspain/PreproundUI` | `PreproundWeb` |

## Рішення

### 1. Платформи

| Що | Де | Налаштування | Адреса |
|---|---|---|---|
| Фронт | **Cloudflare Pages**, репо `PreproundUI`, гілка `master` | команда `npm run build`, вихідна тека `dist/web/browser`, Node з `.node-version` (22.23.5) | `https://prepround.pages.dev` (проєкт `prepround`; за замовчуванням Pages запропонує `preproundui`) |
| API | **Render**, Free web service, Docker, регіон Frankfurt | Root Directory `backend`, Dockerfile `./Dockerfile`, контекст `.`, Health Check Path `/api/health` | `https://prepround-api.onrender.com` |
| БД | **Neon** Free, AWS Europe Central 1 (Frankfurt), Postgres 18, БД `prepround` | — | рядки підключення має лише власник |

API й БД стоять в одному місті, тож затримка між ними мінімальна. Міграція `InitialCatalog` використовує лише `text`, `integer`, `boolean`, `jsonb` і `timestamptz`, без сирого SQL. Тому годиться будь-який Postgres від 14, і версія Neon нічого не блокує.

**Деплой — це push у `master`.** Обидва сервіси збирають код самі.

Root Directory `backend` дає дві речі:

- Render не бачить файлів поза цією текою;
- Render не перезбирає сервіс, коли комміт змінює лише `content/` (render.com/docs/monorepo-support).

Build filters (шляхи від кореня репо) додатково ігнорують `backend/.claude/**` і `backend/tests/**`. Хвилі контенту часто правлять беклог, а на Hobby лише 500 хв збірки на місяць.

### 2. Конфігурація: що в git, а що в змінних

- **У git** лежить усе, що не секрет. Його видно в рев'ю, і його перевіряють тести:
  - `backend/src/Prepround.Api/appsettings.Production.json`: `Cors:Origins`, заголовок з IP клієнта, рівень логів EF Core;
  - `PreproundWeb/src/environments/environment.production.ts`: адреса API.
- **`http://localhost:4300` переїжджає в `appsettings.Development.json`.** Масиви конфігурації зливаються за індексом, тож базовий origin не має потрапити в Production.
- **У змінних Render** лежить лише секрет `ConnectionStrings__Prepround`, тобто рядок ролі лише для читання (розділ 5). Інших змінних не потрібно:
  - `ASPNETCORE_ENVIRONMENT=Production` задає Dockerfile;
  - порт береться з `PORT`, який Render передає сам.

Головна сесія пропонувала задавати CORS змінною `Cors__Origins__0` у Render. PM вибрав файл у git: менше ручних кроків, а значення перевіряє автотест. Перевизначити його змінною однаково можна: змінні середовища мають вищий пріоритет за json.

### 3. Образ API

- Multi-stage. Збірка на `mcr.microsoft.com/dotnet/sdk:10.0`, запуск на `mcr.microsoft.com/dotnet/aspnet:10.0`.
- **Образ запуску з ICU.** Тег `10.0` — це Ubuntu 24.04 з ICU 74 (перевірено запуском 2026-11-19). Варіанти `-alpine` і `-chiseled` відкинуто: у них немає ICU, і `StringComparer.InvariantCulture` став би ordinal, тобто змінився б порядок тем у каталозі (хвіст PRP-005).
- **Без root.** `USER $APP_UID` (1654).
- **Production у самому образі:** `ASPNETCORE_ENVIRONMENT=Production`. Тому OpenAPI і developer exception page на проді вимкнені.
- **Порт:** застосунок слухає `${PORT:-8080}`. Render передає `PORT` (за замовчуванням 10000), локально без `PORT` — 8080. Код хостингу в `Program.cs` не потрібен.
- **TLS** завершує балансувальник Render і пересилає запит у контейнер по HTTP. `UseHttpsRedirection` лишається: з `X-Forwarded-Proto: https` він не редиректить. Без HTTPS-порту в Production він лише раз пише попередження.
- **Міграції на старті не застосовуються.** Інакше Render мав би рядок ролі-власника БД (розділ 5).

### 4. IP клієнта за проксі (rate limit 30/хв, spec 5.2)

**Що відомо (2026-11-19, офіційної документації Render немає):**

- Render стоїть за Cloudflare.
- `X-Forwarded-For` приходить у вигляді `<що надіслав клієнт…>, <IP клієнта>, <IP вузла Cloudflare>`. Proxy Render клієнтські значення не прибирає, а лише дописує свої. `RemoteAddr` — приватна IP проксі Render.
- Cloudflare кладе одну IP клієнта в `CF-Connecting-IP` і сам радить читати саме цей заголовок, а не `X-Forwarded-For`.

**Висновок.** Варіант «`ForwardLimit = 1` по `X-Forwarded-For`» (найправіший запис) на Render дав би IP вузла Cloudflare. Тоді ліміт ділився б між випадковими групами людей, а не між людьми.

**Рішення:**

- Forwarded headers middleware стоїть першим у конвеєрі.
- `KnownProxies` і `KnownIPNetworks` очищено. Render IP своїх проксі не публікує, а до контейнера можна дістатися лише через них. `KnownNetworks` у .NET 10 obsolete (ASPDEPR005).
- `ForwardedForHeaderName` і `ForwardLimit` беруться з конфігурації `ForwardedHeaders`. У Production це `CF-Connecting-IP` і `1`.
- **Запасний варіант** — `X-Forwarded-For` і `ForwardLimit = 2`, тобто другий запис справа. Вмикається лише змінними Render, без нової збірки.
- **Перевірка — лише на живому сервісі (PRP-024):**
  - T1: підроблені `X-Forwarded-For` і `CF-Connecting-IP` не обходять ліміт;
  - T2: поки ліміт вичерпано з Мака, телефон у мобільній мережі тест починає.

  Обидві перевірки разом відрізняють правильне налаштування від трьох хибних: за підробленим заголовком, за IP вузла Cloudflare і за IP проксі Render.

### 5. Доступ до БД і секрети

- **Дві ролі Neon:**
  - `neondb_owner` — для міграцій та імпорту з терміналу власника;
  - `prepround_reader` (лише `SELECT`) — для Render.

  API нічого не пише: `SaveChanges` є лише в імпорті ContentTool (перевірено grep 2026-11-19). Тож навіть зламаний API не змінить даних, а рядок власника ніколи не потрапляє на сервер.
- **Роль читача створюється SQL-ом.** Роль, створена в консолі Neon, отримує `neon_superuser`. SQL для ролі (документація Neon, «Manage roles»):

  ```sql
  CREATE ROLE prepround_reader WITH LOGIN PASSWORD '<openssl rand -hex 24>';
  GRANT CONNECT ON DATABASE prepround TO prepround_reader;
  GRANT USAGE ON SCHEMA public TO prepround_reader;
  GRANT SELECT ON ALL TABLES IN SCHEMA public TO prepround_reader;
  ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO prepround_reader;
  ```

  Останній рядок треба виконати від тієї ж ролі, яка запускає міграції (`neondb_owner`). Інакше таблиці з майбутніх міграцій читач не побачить.

  **Фактичні назви (PRP-024, 2026-11-20):** БД називається `preprounddb`, а роль власника — `preprounddb_owner`. Її Neon створив разом із БД, тож `neondb_owner` і `prepround` у цьому розділі слід читати як ці назви. SQL виконано з `preprounddb` від `preprounddb_owner`.
- **Формат рядка.** Npgsql не розуміє URI `postgresql://…`, який показує Neon, тому потрібен key-value формат:

  ```
  Host=<ep-…>.eu-central-1.aws.neon.tech;Database=prepround;Username=<роль>;Password=<…>;SSL Mode=VerifyFull;Channel Binding=Require
  ```

  `VerifyFull` і `Channel Binding` радить Neon. У рядку для Render додається `Connection Idle Lifetime=60`. Npgsql за замовчуванням тримає простійне з'єднання 300 с, а Neon засинає після 5 хв. Документація Neon не каже, чи рахуються простійні з'єднання за активність, тож коротший термін прибирає гонку «пул віддав з'єднання, яке Neon уже закрив».
- **Обидві ролі ходять на direct endpoint (без `-pooler`).** Для міграцій pooled не годиться, а API — один довгоживучий процес із власним пулом Npgsql. Pooled endpoint варто розглянути, якщо з'являться помилки через ліміт з'єднань.
- **Рядки підключення не потрапляють у чат, агентам, у git і в `.env`-файли.** Власник вводить рядок через `read -rs`, тому його немає ні на екрані, ні в історії shell, і передає лише змінною середовища для однієї команди. Сценарій Neon для агентів (`npm i -g neon`, `neon init`/`neon deploy`, `neon.ts`, `.env.local`) не використовуємо: він для Node-застосунків.
- **Бекапів не робимо.** БД повністю відтворюється з git: міграції плюс `import`. Neon Free і так тримає 6 годин історії.

### 6. Контент у проді: лише перевірені теми

На 2026-11-19 у `content/` лежить 84 питання з 7 тем без PASS. Серед них `pinia-003`, де фактчек уже знайшов хибну правильну відповідь. Хвилі весь час дописують нові теми, а `import` бере всю теку. Фактчек знаходив `critical` приблизно в кожній шостій темі: `sql-010`, `laravel-010`, `php-lang-012`, `pinia-003`.

**Рішення (PRP-023):**

- **Позначка в даних, а не в пам'яті:** необов'язкове поле теми `draft: true`.
- **Імпорт за замовчуванням безпечний.** `import` без прапорця не бере питань тем-чернеток: вони вважаються відсутніми, тож раніше імпортовані деактивуються. Dev-імпорт хвиль іде з `--include-drafts`. Якщо забути прапорець, у dev просто не буде чернеток (видно одразу), а не з'являться неперевірені питання в проді.
- **Хто знімає позначку.** Нова тема з'являється чернеткою: усі порожні теми отримують `draft: true` вже зараз. Знімає `draft` лише головна сесія після PASS теми. Автори позначку не чіпають.
- **Той самий механізм — аварійне вимкнення.** Якщо в опублікованій темі знайдено хибну відповідь, тему знову позначають `draft: true` і запускають `import` у прод: її питання зникають із тестів, поки триває виправлення.

### 7. Зміна процесу після деплою

1. **Міграція.** Власник застосовує нову EF-міграцію в Neon **до** push коду, який від неї залежить, бо Render деплоїть одразу після push.
2. **Імпорт у прод** — крок власника, без `--include-drafts`, один за раз (два одночасні `import` можуть зіткнутися на PK, хвіст PRP-004).
   - Коли: на зупинках хвиль і етапів, а не після кожної теми.
   - Перед імпортом: `git pull`, щоб імпортувати закомічений стан. Після імпорту — короткий smoke `GET /api/catalog`.
3. **Агенти й головна сесія з Neon не працюють.** Smoke проду — лише `curl` до публічних адрес.
4. **Перейменування.** Якщо назви сервісів інші, змінюються рівно два файли: `environment.production.ts` і `appsettings.Production.json`.

## Компроміси й ризики

- **Холодний старт.** Render Free засинає після 15 хв без запитів і прокидається приблизно за хвилину. Майже кожен перший відвідувач MVP це відчує. Тому PRP-022 через 5 с показує під «Завантаження…» пояснення. Пінг для утримання — рішення власника («Ідеї»). На місяць є 750 год, що покриває один сервіс 24/7. *Виміряно 2026-11-20: ~25 с, а не хвилина (Render 14 с + перший запит до БД 11 с, «Факти після деплою»).*
- **Трафік Render.** На Hobby з 2026-08-01 входить 5 ГБ вихідного трафіку на місяць. Без способу оплати сервіс зупиняється, коли трафік вичерпано. Виміряно 2026-11-19, без стиснення (стиснення відповідей API немає):
  - каталог — 13,7 КБ;
  - тест на 20 питань — 23,4 КБ.

  Тобто приблизно 140 тис. сесій на місяць. Brotli для API — в «Ідеях».
- **Ліміти Neon Free:** 1 ГБ, 100 CU-год, 5 ГБ egress на проєкт за місяць, сон після 5 хв (вимкнути не можна). Прокидається за сотні мілісекунд. Регіон проєкту після створення не змінюється.
- **Збірки.** Render кешує образи з публічних реєстрів, тож плаваючий тег `10.0` може відставати від свіжого патча. Pin за digest і Renovate — в «Ідеях».
- **Cloudflare радить для нових проєктів Workers Static Assets.** Pages працює, примусового дедлайну не знайдено (2026-11-19). Якщо в панелі не буде Pages, зупинка: для Workers SPA-режим задається в `wrangler.jsonc` (`not_found_handling`).
- **Preview-деплої Pages мають інші піддомени й CORS не пройдуть.** Це прийнятно.
- **IP клієнта на Render ніде не задокументовано** (розділ 4): поведінку перевіряємо на живому сервісі, а запасний варіант вмикається без збірки.

## Відкинуто

| Варіант | Причина |
|---|---|
| Google Cloud Run | потрібна картка й білінг, безкоштовні регіони лише в США |
| Oracle Always Free | адміністрування VM самому, потрібна картка; з серпня 2026 ARM-квоту урізали вдвічі, простійні VM забирають |
| Azure App Service F1 | 60 CPU-хв на добу, без власного домену |
| Koyeb | після купівлі Mistral (лютий 2026) безкоштовного плану для нових користувачів немає |
| Fly.io | free tier прибрали 2024 року |
| Railway | лише $1 на місяць |
| Render Postgres Free | безкоштовна БД видаляється через 30 днів |
| Supabase | пауза проєкту після тижня без активності |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` | бере найправіший `X-Forwarded-For`, тобто IP вузла Cloudflare; назву заголовка так не змінити |
| `ForwardLimit = 1` по `X-Forwarded-For` | та сама причина (розділ 4) |
| `EF Database.Migrate()` на старті | Render мав би рядок ролі-власника; міграція при кожному холодному старті |
| Імпорт контенту при деплої | той самий секрет на сервері; на Render Free немає pre-deploy команди (в «Ідеях» як GitHub Action) |
| Pooled endpoint Neon для API | зайва ланка для одного процесу з пулом Npgsql; повернутись, якщо впремося в ліміт з'єднань |
| Neon CLI / `neon deploy` / `.env.local` | сценарій для Node; секрети опинились би у файлах репо |

## Метрики для портфоліо (заповнює PRP-024)

Що міряти: розмір образу, час холодної й повторної збірки локально та на Render, час холодного старту (`curl -w '%{time_total}'` після 15+ хв простою), TTFB каталогу «теплого» API, кількість кроків ручного деплою.

### Факти після деплою (2026-11-19 / 2026-11-20)

| Що | Значення |
|---|---|
| Фронт | https://prepround.pages.dev (Cloudflare Pages, `master` репо `PreproundUI`) |
| API | https://prepround-api.onrender.com (Render Free, Docker, Frankfurt) |
| БД | Neon Free, Frankfurt, PostgreSQL 18.7, БД `preprounddb` |
| Контент у проді | 61 тема, 564 питання (без чернеток, PRP-023) |
| Образ API | 297 МБ (`aspnet:10.0`, Ubuntu 24.04), локальна збірка |
| Health «теплого» API | 0,18–0,41 с |
| Каталог «теплого» API | 0,60 с, 13,8 КБ |
| `POST /api/tests` (10 питань) | 1,46 с |
| Холодний старт (ранок 2026-11-20, після ночі простою) | `/api/health` — 14,0 с (пробудження Render); перший `/api/catalog` одразу після нього — 11,3 с (пробудження Neon і перший запит EF). Разом людина чекає ~25 с |
| Ручні кроки деплою | 4: міграція й імпорт у Neon, роль читача, сервіс Render (одна змінна середовища), проєкт Pages (дві команди). Далі push у `master` перезбирає обидва сервіси сам |
| Перевірки | T1 (підробка IP не обходить ліміт) — PASS; Cloudflare відкидає `CF-Connecting-IP` від клієнта (`403`, `error code: 1000`). Власник пройшов тест із ПК і телефона — без помилок |

Час збірки на Render не виміряно: його видно в Render → Events.

## Джерела (перевірено 2026-11-19)

- Render: render.com/docs/free, /web-services (PORT, 0.0.0.0, TLS), /docker, /monorepo-support, /health-checks, /environment-variables.
- IP за проксі Render: feedback.render.com/features/p/send-the-correct-xforwardedfor; стаття «Why does request.remote_ip work on Heroku but not Render?» (rohitpaulk.com). Обидва джерела неофіційні, тому потрібна перевірка T1/T2.
- Cloudflare: developers.cloudflare.com/fundamentals/reference/http-headers/ (`CF-Connecting-IP`, дописування `X-Forwarded-For`), /pages/configuration/serving-pages/ (SPA без `404.html`), /pages/configuration/build-image/ (v3, Node 22.23.4 за замовчуванням, `.node-version`).
- Neon: neon.com/docs/postgresql/postgres-version-policy (14–18), /introduction/plans, /guides/dotnet-npgsql, /manage/roles, /introduction/scale-to-zero, /docs/introduction/regions.
- ASP.NET Core 10: learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer (порядок справа наліво, `ForwardLimit`, `ForwardedForHeaderName`); застарілі `KnownNetworks`/`IPNetwork` (aspnet/Announcements #523, ASPDEPR005); образи .NET 10 на Ubuntu за замовчуванням (learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu).
- Запуском: `docker run mcr.microsoft.com/dotnet/aspnet:10.0` → Ubuntu 24.04.4, `libicuuc.so.74`, `APP_UID=1654`, `ASPNETCORE_HTTP_PORTS=8080`. Розміри відповідей — `curl` до локального API.
