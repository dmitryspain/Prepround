# Prepround: підготовка до співбесід

Тести для підготовки до співбесід. Людина вибирає напрям, стек, рівні, кількість питань і розподіл по темах, проходить тест із таймером і бачить, які теми провисають. Живе на **https://prepround.pages.dev** (API — `prepround-api.onrender.com`, БД — Neon), українською й англійською.

- Специфікація (джерело правди для поведінки): `backend/.claude/pm/spec.md`.
- Рішення (ADR): `backend/.claude/pm/decisions/` — 001 стек і архітектура, 002 формат контенту, 003 фронт в окремому репо, 004 джерела контенту, 005 деплой, 006 дві мови.
- Беклог: `backend/.claude/pm/backlog.md` (виконані картки — `backlog-archive.md`).
- **Головна сесія** (не субагенти) веде роботу за `backend/.claude/pm/process.md`: «погнали», хвилі, деплой, git, економія токенів. Субагентам цей файл читати не треба.

## Структура (два репо)

| Де | Що там |
|---|---|
| `Prepround/backend/` | ASP.NET Core 10 Web API, PostgreSQL 18, `Prepround.slnx` |
| `Prepround/content/` | питання в YAML: `content/<галузь>/field.yaml`, `content/<галузь>/topics/<тема>.yaml` |
| `Prepround/backend/.claude/` | інструкції, агенти, spec, беклог, рішення — для обох репо |
| `/Users/dmitryspain/RiderProjects/PreproundWeb` | окремий git-репо: Angular 22 (веб); iOS відкладено (PRP-008) |

Область картки `backend` / `content` → репо `Prepround`, `frontend` → `PreproundWeb` (працюй за абсолютними шляхами). Шляхи в картці — відносно свого репо, у звітах — з префіксом репо. Сесію запускаємо з `Prepround/backend/`, інакше Claude Code не бачить агентів.

## Продукт коротко (деталі в spec.md)

- Каталог: галузь (IT) → шкала рівнів → напрями (Backend, Frontend) → стеки. Стек — лише набір тем за замовчуванням; тему можна додати будь-яку. Теми належать галузі, не стеку.
- **Рівні — дані, не код:** шкалу задає `field.yaml`; назв рівнів у коді немає.
- Тест: рівні, 10–100 питань, розподіл по темах (максимум теми = активні питання на вибраних рівнях мовою сайту), таймер 60 с × питання. `single` — 1 правильний, `multiple` — ≥ 2, зараховуємо лише повністю правильну відповідь. Варіанти перемішуються, пояснення обов'язкове. «Перевірити» показує правильні варіанти й блокує відповідь.
- Реєстрації немає: історія й «вже бачені» — на пристрої. Відповіді перевіряє клієнт (рішення 001).
- Контент пише `content-author` за офіційною документацією (рішення 004); застосунок читає питання лише з БД, куди їх кладе `ContentTool import` (рішення 002).
- Дві мови (ADR 006): інтерфейс і контент uk/en, мова контенту — `?lang=` в API; англійський тест бере лише перекладені питання.

## Архітектура бекенду

Clean Architecture, CQRS-хендлери без MediatR, стандартний DI (рішення 001).
- `Prepround.Domain` — сутності й `Result`/`Error`; `Prepround.Application` — запити/команди/хендлери (`Abstractions/Messaging`), вибір питань, валідація контенту, абстракції репозиторіїв; `Prepround.Infrastructure` — EF Core + Npgsql, міграції, репозиторії, YAML, імпорт; `Prepround.Api` — тонкі контролери, CORS, rate limiting; `Prepround.ContentTool` — `validate` / `import`.
- `tests/Prepround.Tests`: xUnit v3, ручні фейки без мок-бібліотек, інтеграційні — `WebApplicationFactory` + Testcontainers.

## Конвенції

- Хендлер повертає `Result` / `Result<T>`; помилка `new Error("Area.Code", "Повідомлення українською.")`. Нова фіча = `XxxQuery`/`XxxCommand` (record) + `XxxHandler` у папці фічі + тонкий екшен. Залежності — через конструктор у `private readonly _name`.
- Коментарі й XML-doc англійською, коротко, «чому». Тексти інтерфейсу — лише в `PreproundWeb/src/app/core/i18n/messages/` (uk, en); `Error.Message` і повідомлення ContentTool українською (для розробника).
- **Нічого з каталогу не хардкодимо:** у `src` обох репо немає `junior`, `middle`, `dotnet`, `ef-core` тощо як ідентифікаторів.
- Id контенту стабільні й не перевикористовуються; зникле питання імпорт деактивує. Випадковість — лише через шов з DI. Час — UTC (`timestamptz`). Акаунтів і персональних даних немає.
- **Секрети** лише в user-secrets / змінних середовища (виняток: dev-рядок локального `prepround-postgres` в `appsettings.Development.json`). Агенти до Neon не ходять; прод перевіряють лише `curl` до публічних адрес.
- Схему БД змінюємо лише новою EF-міграцією, старі не редагуємо. Пакети — лише дозволені карткою або рішенням 001; версії NuGet лише в `Directory.Packages.props`; `backend/nuget.config` не прибирати.

## Фронтенд (`PreproundWeb`, шляхи від його кореня; там свій CLAUDE.md)

- Angular 22: standalone, сигнали, zoneless, Vitest, SCSS; без NgRx і UI-бібліотек. Кольори лише через токени тем (`var(--…)`); `matchMedia` — через токен `MEDIA`; верстка спершу під телефон.
- HTTP і маппінг DTO — лише `core/api.service.ts`, моделі — `core/models.ts`; адреса API — токен `API_BASE_URL` (`environment.ts` / `environment.production.ts`). `ApiService` сам додає `lang`.
- Локальні дані — лише через `core/storage.ts` (ключі `prepround.history.v1`, `prepround.prefs.v1`). Мову пише лише `PrefsService`; `navigator.languages` — через `BROWSER_LANGUAGES`.
- Текст контенту не вставляємо через `[innerHTML]`; inline-код рендерить pipe, що спершу екранує.
- **Правило контракту (рішення 003):** зміна ендпоінта/DTO/формату в тій самій картці оновлює бекенд і `api.service.ts`/`models.ts`; джерело — spec, розділ 5. Tester проганяє тести обох репо.

## Порти

API `https://localhost:7120` (фронт ходить лише сюди) · Postgres `localhost:5433` (`prepround-postgres`) · dev server `http://localhost:4300`. API і dev server тримає запущеними головна сесія — другого екземпляра не запускай.

## Команди

```bash
# з Prepround/backend/
docker compose up -d                       # Postgres на 5433
dotnet build && dotnet test                # MTP: фільтри після `--`, напр. dotnet test -- --filter-class Prepround.Tests.Api.CorsTests
dotnet ef migrations add <Name> --project src/Prepround.Infrastructure --startup-project src/Prepround.Api
dotnet run --no-build --project src/Prepround.ContentTool -- validate ../content          # 0 ок, 1 помилки, 2 аргументи
dotnet run --no-build --project src/Prepround.ContentTool -- import ../content [--include-drafts]   # 3 — БД; рядок: ConnectionStrings__Prepround
python3 .claude/tools/content-lint.py <тема…> | --all [--urls] [--base <копія content> [--translation-only]]
# dev-рядок: ConnectionStrings__Prepround="Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=prepround"

# з /Users/dmitryspain/RiderProjects/PreproundWeb
npx ng test --watch=false && npm run build
```

ContentTool агенти запускають лише з `--no-build` (збирає головна сесія: паралельні збірки ламають одна одну).

## Агенти й звіти

| Агент | Роль |
|---|---|
| `coder` | продакшн-код рівно під картку, без тестів і без розширення скоупу |
| `content-author` | питання в `content/` за офіційною документацією (рішення 004), перевірка запуском |
| `tester` | перевіряє зміну, пише тести, фактчекає контент, вердикт PASS / FAIL / BLOCKED |
| `project-manager` | планує й пріоритизує картки, веде беклог і `pm/knowledge.md` |

- Картки — `PRP-NNN` з полями «Область» і «Платформа». Картки з «Виконавець: власник» агентам не дають.
- **Звіти (економія токенів, рішення власника 2026-11-26):** повний звіт у форматі зі свого визначення агент пише у файл, який назвала головна сесія (за замовчуванням `backend/.claude/reports/<PRP-або-тема>-<роль>.md`, тека в `.gitignore`). Відповідь головній сесії — **коротка, до ~15 рядків**: вердикт, змінені файли, пункти FAIL / блокери, шлях до повного звіту. Вхідні дані (картку, звіт попереднього агента) агент читає з файлів за шляхами з промпта.
- Субагенти не роблять `git commit` / `push`; комітить головна сесія.
