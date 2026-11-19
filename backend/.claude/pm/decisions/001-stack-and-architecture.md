# 001 · Стек і архітектура

- **Статус:** accepted (2026-11-13). Розділ «Репозиторій» і пункт «Окремі репо…» у «Відкинуто» замінено рішенням 003 (2026-11-14): фронт живе в окремому репо `PreproundWeb`.
- **Хто вирішив:** власник погодив стек «як в Outlay» (ASP.NET Core + PostgreSQL + Angular + Capacitor, веб + iOS), відмову від реєстрації в MVP і монорепо `Prepround`. Деталі нижче запропонувала головна сесія, і їх можна переглянути новим ADR.
- **Версії перевірено наживо 2026-11-13:** release-metadata .NET, npm registry, NuGet, Docker Hub, nodejs.org.

## Рішення

### Репозиторій

Монорепо `/Users/dmitryspain/RiderProjects/Prepround` з папками `backend/`, `frontend/`, `content/` і `.claude/`. Власник попросив один репозиторій. Контракт API і контент змінюються в одній картці з кодом, тож розійтися їм ніде.

### Бекенд

| Що | Вибір | Чому |
|---|---|---|
| Платформа | **.NET 10 LTS** (`net10.0`), SDK 10.0.404 / runtime 10.0.15 | .NET 8 і 9 вийшли з підтримки 2026-11-10. .NET 11 (STS) підтримується так само до 2028-11-14, але Npgsql для EF Core 11 ще не вийшов (останній — 10.0.3) |
| API | ASP.NET Core, контролери | як в Outlay: власник і агенти знають патерн |
| CQRS | власні `IQuery<T>` / `ICommand<T>` + хендлери, **без MediatR** | MediatR 13+ має комерційну ліцензію. Для кількох хендлерів посередник не потрібен |
| DI | стандартний `Microsoft.Extensions.DependencyInjection`, **без Autofac**. В Application — лише `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.15 для `AddApplication()` (дозволено 2026-11-14 після PRP-002) | модулів мало, а зайва залежність ускладнює підтримку |
| БД | PostgreSQL **18** (`postgres:18-alpine`, остання стабільна 18.7; 19 поки RC), порт 5433 | 5432 зайнятий контейнером Outlay |
| ORM | EF Core 10.0.15 + Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 | |
| YAML | YamlDotNet 18.1.0 | |
| OpenAPI | Microsoft.AspNetCore.OpenApi 10.0.15, документ `/openapi/v1.json` лише в Development, без UI-пакета | документ можна імпортувати в Postman |
| Тести | xunit.v3 4.0.3, Microsoft.AspNetCore.Mvc.Testing 10.0.15, Testcontainers.PostgreSql 4.16.0, без мок-бібліотек. `dotnet test` у режимі Microsoft Testing Platform (`global.json` → `"test": {"runner": "Microsoft.Testing.Platform"}`): xunit.v3 4.x на .NET 10 SDK у режимі VSTest не запускається (PRP-002) | ручні фейки, як в Outlay |
| Інструменти | локальний маніфест `backend/.config/dotnet-tools.json` з dotnet-ef 10.0.15 | глобальний dotnet-ef у власника 9.0.4 |
| `global.json` | `"version": "10.0.100"`, `"rollForward": "latestFeature"`, без prerelease; секція `test` — див. «Тести» | збирається на встановленому SDK і бере новіший, коли власник оновиться |
| Пакети | центральне керування версіями (`backend/Directory.Packages.props`, transitive pinning EF Core 10.0.15) і `backend/nuget.config` з єдиним джерелом nuget.org та package source mapping (PRP-002) | на машині власника глобально є друге NuGet-джерело: без `nuget.config` restore падає з `NU1507`, а одне джерело ще й захищає від dependency confusion |
| Rate limiting | вбудований `Microsoft.AspNetCore.RateLimiting` | |

### Фронтенд

| Що | Вибір | Чому |
|---|---|---|
| Фреймворк | **Angular 22** (22.2.x), стандартні налаштування `ng new`: standalone, zoneless, **Vitest** (типовий test runner у v22), SCSS, без SSR | актуальна мажорна версія. Outlay на Angular 17 — уже стара |
| TypeScript | версія, яку ставить Angular CLI (Angular 22 вимагає `>=6.0 <6.1`) | TypeScript 7 Angular 22 не підтримує |
| Стан | сигнали в сервісах, без NgRx | стану мало: налаштування, поточний тест, історія |
| UI | без UI-бібліотеки, власні компоненти, CSS-змінні, світла й темна тема за системою | нативний вигляд на iOS без «матеріального» стилю, менше залежностей |
| Node | **22.23.5 LTS** (мінімум для Angular 22 — 22.22.3) | лишаємось на 22-й гілці, щоб не зламати Angular 17 в Outlay. Підтримка Node 22 закінчується **2027-04-30**, тож перехід на Node 24/26 обговорюємо до лютого 2027 |
| iOS | **Capacitor 8.6.x** (`@capacitor/core`, `@capacitor/cli`, `@capacitor/ios`), SPM, iOS 17+, Bundle ID `io.github.dmitryspain.prepround` | як в Outlay (рішення 001 Outlay) |

### Дані користувача

- **Реєстрації немає.** Історія результатів і «вже бачені» питання зберігаються на пристрої, у `localStorage` через абстракцію `core/storage.ts`. На iOS її пізніше можна замінити на `@capacitor/preferences`, не чіпаючи решту коду.
- Сервер не зберігає нічого про користувача. Захист — rate limiting і ліміти розміру запиту.
- **Відповіді перевіряє клієнт.** Сервер віддає тест разом із правильними відповідями й поясненнями. Підробити результат можна, але обманює людина лише себе. Натомість не треба зберігати тести на сервері, а перевірка працює миттєво.

### Порти

API `https://localhost:7120` і `http://localhost:5120`, Postgres `5433`, Angular `4300`. Порти Outlay (7016, 59192, 5432, 4200) не зайняті.

## Відкинуто

- **.NET 11:** немає Npgsql 11, а строк підтримки той самий, що в .NET 10. Станом на 2026-11-14 (NuGet): EF Core 11.0.0 вже GA, але Npgsql.EntityFrameworkCore.PostgreSQL — лише 11.0.0-rc.3, остання стабільна 10.0.3. Рішення в силі.
- **MediatR / Autofac:** ліцензія і зайва складність.
- **Окремі репо для бекенду, фронту й контенту:** власник хоче один репозиторій.
- **Серверна історія з анонімним id пристрою:** не потрібна без акаунтів, а коли акаунти з'являться, локальну історію можна перенести.

## Наслідки

- Перед каркасом фронту власник має оновити Node (PRP-001). Бекенд від цього не залежить.
- Перехід на акаунти потребуватиме нового ADR: зберігання історії на сервері, міграція локальних даних.
