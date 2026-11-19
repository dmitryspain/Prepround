# Беклог Prepround

> Веде агент `project-manager`. Формат картки описано в `backend/.claude/agents/project-manager.md`. Нумерація наскрізна (PRP-001, PRP-002, …).
> Поведінка продукту — у `backend/.claude/pm/spec.md`, рішення — у `backend/.claude/pm/decisions/`. У картці coder, яка в роботі, змінюю лише статус.
> Статус `deferred` означає, що власник відклав картку. Такі картки в «Готовність карток» не потрапляють.
> **Два репозиторії (рішення 003):** «Область» `backend` і `content` — це `Prepround` (`/Users/dmitryspain/RiderProjects/Prepround`), `frontend` — це `PreproundWeb` (`/Users/dmitryspain/RiderProjects/PreproundWeb`). Шляхи в картці відносно кореня свого репо. Якщо картка зачіпає обидва, шляхи мають префікс `Prepround/` або `PreproundWeb/`.

## Готовність карток (2026-11-19, етап 5 «Англійська версія», ADR 006)

- **Пріоритет зараз — дві мови сайту** (рішення власника 2026-11-19, `backend/.claude/pm/decisions/006-languages-uk-en.md`, accepted). Перекладаються інтерфейс, назви каталогу і питання.
- **Перед PRP-027 головна сесія** вносить у spec і CLAUDE.md правки з ADR 006, розділ 9: spec 3.1, 3.5, 3.7, 5, 5.1, 5.2, 6. PRP-025 і PRP-026 від spec не залежать, тож їх можна починати одразу.
- **У роботі:** PRP-024 (етап 4a). Чекає власника: T2 з телефона, перевірка на телефоні й десктопі, факти для портфоліо в ADR 005. Етап 5 він не блокує.
- **Готово для coder зараз (по черзі, послідовно):**
  1. PRP-025 — ключі перекладу в YAML і правило 11 у `validate` (лише бекенд, без БД);
  2. PRP-026 — міграція `ContentTranslations` і `import` перекладів;
  3. PRP-027 — словники інтерфейсу, перемикач і вибір мови (`PreproundWeb`);
  4. PRP-028 — `?lang=` в API, фронт шле мову й перезавантажує каталог (обидва репо);
  5. PRP-029 — `content-lint` для англійської, `--base`, `--translation-only`.
- **Далі, після PRP-029:**
  - PRP-030 (`content-author`) — назви 61 теми англійською, англійські коментарі в коді 18 опублікованих питань;
  - **хвилі перекладу** (розділ «Хвилі перекладу» нижче, бриф `translation-brief.md`), без зупинки після них;
  - PRP-031 — правило 12, англійська в прод-збірці;
  - PRP-032 (власник) — деплой.
- **Власник може робити зараз:**
  - PRP-024 — те, що лишилось;
  - **після PRP-026, до наступного push `backend/`:** застосувати міграцію в Neon (PRP-032, крок 1). Без неї новий код API на Render дасть `500`.

  Push PRP-025 і PRP-027…PRP-030 безпечний у будь-якому порядку: у прод-збірці одна мова. **Push `PreproundWeb` з PRP-031 — лише після прод-імпорту перекладу** (PRP-032, крок 3), інакше англійська з'явиться без питань.
- **Зупинки для власника:**
  - **кінець етапу 5** (після PRP-031): прод-імпорт, `curl`, push `PreproundWeb`, перевірка сайту англійською. Це PRP-032, кроки 3–5;
  - **калібрування без зупинки:** після PASS перших трьох тем перекладу головна сесія пише власнику один рядок з їхніми назвами. Відгук, якщо буде, PM вносить у `translation-brief.md`;
  - після PRP-024 (кінець етапу 4a) і після PRP-012 (етап 2b) — як і раніше.
- **Пауза: хвилі контенту Frontend (з 2026-11-18).**
  - **Чому:** `critical` у `pinia-003`; ліміт API перервав 5 агентів; власник не відповів про правило зупинки.
  - **Відновлення:** за словом власника, після PRP-029 і вже двома мовами (бриф контенту, «Дві мови»). Якщо власник захоче раніше, автори пишуть українською, а англійську цих тем додає окрема хвиля перекладу.
  - Стан тем — «Хвилі контенту», «Frontend, прогрес».
- **Середовище для агентів:**
  - головна сесія тримає запущеними API (`https://localhost:7120`) і dev server (`http://localhost:4300`);
  - агенти не запускають другого екземпляра. Після бекенд-карток із `curl`-критеріями (PRP-028) API перезапускає головна сесія;
  - Docker-контейнери й образи карток мають префікс `prepround-` і прибираються після перевірки.
- **Відкладено, не брати:** PRP-008 (iOS), розділ «Відкладено: iOS».
- **Закрито:**
  - етап 1: PRP-001, PRP-002, PRP-003, PRP-014, PRP-004, PRP-005, PRP-006;
  - етап 2: PRP-007, PRP-009, PRP-010, PRP-011;
  - етап 2a: PRP-015, PRP-016;
  - етап 3: PRP-017, PRP-018, хвилі 1–3. Backend завершено 2026-11-18: 42 теми, 504 питання, PASS;
  - етап 4a: PRP-020, PRP-021, PRP-022, PRP-023. Лишився PRP-024.
  - Хвости етапу 2 розкладено 2026-11-14 (рядок «Розкладено» під кожною карткою). Хвости етапу 4a розкладе PM на закритті PRP-024.

## Хвилі контенту (рішення власника 2026-11-15; CLAUDE.md «Погнали», ADR 004 розділ 8)

По 12 питань на тему (4/4/4), 3 автори паралельно на Sonnet (tester — Opus), тема за темою. Спершу Backend, потім Frontend; потім глибина до 30.

- **Хвиля 1 — PASS** (2026-11-15; 144 питання; фактчек Opus знайшов і виправлено 2 хибні відповіді — `hibernate-jpa-009`, `api-design-011` — та системну перевагу довжини правильних варіантів → `backend/.claude/tools/option-lengths.py`): 1 `azure` → `docker-k8s` → `architecture` → `messaging` · 2 `java-core` → `jvm` → `java-concurrency` → `spring-boot` · 3 `hibernate-jpa` → `aws` → `nosql` → `api-design`. Перша спроба (6 авторів на Opus) двічі вичерпала ліміт без жодного записаного питання.
- **Хвиля 2** (2026-11-15 → 2026-11-17).
  - **Написано:** усі 11 тем, `content-lint --urls` OK, імпортовано (348 питань), `dotnet test` 309 / 0.
  - **Знайдено запуском:** `fastapi-009` мала хибну правильну відповідь, питання переписано; у `python-core-011` уточнено текст помилки під 3.14.
  - **PASS (2026-11-17, з 1 кола):** `python-async`, `pytest`, `celery`. Хибних відповідей немає. 6 зауважень, що не блокували, внесено: `celery-011` (у 5.7 — `RuntimeError`, а не лише deadlock), `pytest-011` (`strict_xfail` з 9.0), `celery-009`, `celery-008`, `python-async-012`, `pytest-004`.
  - **PASS (2026-11-17, з 1 кола):** `javascript`, `typescript`, `nodejs-runtime`, `express-fastify`. Хибних відповідей немає; `typescript-012` перевірено й для Node 26 (`--experimental-transform-types` прибрано у v26.0.0). 6 зауважень, що не блокували, передано автору: формулювання `javascript-008`, правопис у `typescript-009`, якорі `nodejs-runtime`, дистрактор `nodejs-runtime-012`, перетин `express-fastify-001`/`002`, пояснення `express-fastify-011`.
  - **PASS (2026-11-17, з 1 кола):** `python-core`, `django`, `fastapi`, `sqlalchemy`. Перевірено запуском 100 % `senior` і `multiple`; переписаний `fastapi-009` підтверджено у трьох конфігураціях. 6 зауважень, що не блокували, передано авторам: пояснення `django-010`, `sqlalchemy-011`; `sources` і службова фраза в `fastapi-009`; граматика `fastapi-011`; `AsyncAttrs` у тексті `sqlalchemy-009`; слабкий дистрактор `python-core-011`.
  - **Хвиля 2 завершена: 11 тем × 12 = 132 питання, усі PASS з першого кола.**
- **Хвиля 3 — конвеєр** (з 2026-11-17, CLAUDE.md «Хвилі контенту»). Черга авторів:
  1. `nestjs`, `go-lang`, `php-lang` (у роботі з 2026-11-17);
  2. довжини-A: `ef-core`, `sql`, `csharp`;
  3. `prisma-typeorm`, `go-concurrency`, `laravel`;
  4. довжини-B: `dotnet-async`, `dotnet-clr`, `architecture`, `spring-boot`;
  5. `go-runtime`, `symfony`, `go-http`, `php-orm`, `go-db`, `composer-psr`, `phpunit`.

  Після черги — кінець Backend і зупинка для власника.
  - **Довжини A+B — PASS усіх 7 тем (`sql` — з 2 кола).** На колі 2 `sql-010` знову має точний ключ (перевірено запуском на PG 18.7); `ef-core-006` і `spring-boot-005` теж уточнено.
  - **Довжини A+B, фактчек (2026-11-17), коло 1.** PASS: `ef-core`, `csharp`, `dotnet-async`, `dotnet-clr`, `architecture`, `spring-boot`. **FAIL `sql`, коло 1, `critical`:** у `sql-010` вирівняний дистрактор («інші індекси, які планувальник вважає кращими») став правдивою причиною, тож ключ `multiple` хибний (відтворено на PG 18.7). Повернуто автору, урок записано в бриф. За правилом «зупинка на `critical`» нові теми (`composer-psr`, `phpunit`) не запускаються до слова власника.
  - **2026-11-18: `symfony` — PASS з 2 кола; `go-db` — PASS (заміну в `go-db-010` підтверджено); `php-orm` — PASS з 2 кола.** Усі 11 написаних тем хвилі 3 і 7 тем із вирівняними довжинами — PASS. Власник: «продовжуємо де зупинились» — зупинку на `critical` знято, у роботі останні `composer-psr`, `phpunit`.
  - **`go-runtime`, `go-http` — PASS з 1 кола (2026-11-18)**: автори внесли зауваження; у `go-http-012` пояснення оновлено під автоматичне дочитування тіла в Go 1.27 (до ~256 КіБ). **`go-db` — PASS з 1 кола**, а заміна правильного варіанта в `go-db-010` (`SetConnMaxIdleTime`) на перевірці. **`php-orm` — PASS з 2 кола** (прибрано звіти автора з 6 пояснень, умову 006 доповнено `$fillable`). **`symfony` — FAIL, коло 1, major** (хибний механізм у поясненні 011, неповна умова 012), правки в автора.
  - **`laravel`, `prisma-typeorm` — PASS з 2 кола (2026-11-18).** `laravel-010` з `--tries=3` перевірено запуском, `prisma-typeorm-007` — через `tsc` на Prisma 7.10.1.
  - **`nestjs` — PASS (2026-11-18, з 1 кола)**, запуском перевірено 9 з 12. **`laravel` — FAIL, коло 1, `critical`:** у `laravel-010` правильна відповідь хибна за умов питання, бо за типового `--tries=1` друге резервування дає `MaxAttemptsExceededException`, а не друге виконання (відтворено на Laravel 13.42); буде додано `--tries=3`. **`prisma-typeorm` — FAIL, коло 1, major:** фрагмент коду в 007 не компілюється, тож дистрактор частково правдивий; пояснення 010 перекручує READ COMMITTED; передумова 011 не відповідає TypeORM 1.1. Правки повернуто авторам.
  - **`go-lang`, `go-concurrency`, `php-lang` — PASS з 2 кола** (2026-11-18): усі пункти внесено, `php-lang-012` тепер з одною правильною відповіддю, перевірено запуском. Рішення головної сесії: `php-lang-010` (поведінка не задокументована на php.net) видалено, замість нього додано `php-lang-013` (`WeakMap` проти `SplObjectStorage`, PASS з 3 кола, запуск `101`); id не перевикористано.
  - **Фактчек `go-lang`, `go-concurrency`, `php-lang` (2026-11-17, коло 1) — FAIL.** Пункти tester (внесено на колі 2):
    - **`php-lang-012` [critical]:** варіант `h(Dog $a): Animal` насправді сумісний, тож у `single` дві правильні відповіді. Замінити на `h(Dog $a): ?Animal` або `h(Dog $a): object` (обидва дають Fatal, перевірено) й оновити пояснення.
    - **`php-lang-011` [minor]:** пояснення про `__set` — у класі немає методу, тож явний виклик дає `Error`.
    - **`go-concurrency-011` [major]:** текст не ставить питання «так/ні», а варіанти починаються з «Так/Ні»; переформулювати.
    - **`go-concurrency-010` [minor]:** пояснення про «оптимізатор»; якір `#restrictions` → `https://go.dev/ref/mem#badsync`.
    - **`go-concurrency-011`/`012` [minor]:** прибрати з пояснень звіти автора про запуски.
    - **`go-lang-009`:** «некомпільованого» → «непорівнюваного». **`go-lang-011`:** прибрати «Я перевірив…». **`go-lang-010`:** «Трете» → «Третє».
    - Не блокує: `php-lang-010` (заміну винятку в `finally` на php.net не описано, bug #68270; підтверджено лише запуском — вирішити, чи тримати); `php-lang-009` (друге джерело про `{ get; }` в інтерфейсі).
  - **На момент зупинки (2026-11-17):**
    - **ще працювали:** автор довжин-A (правка `sql-010`, `ef-core-006`), автор довжин-B (`spring-boot-005`), автори `go-db` і `php-orm`, tester `nestjs`/`prisma-typeorm`/`laravel`;
    - **чекають фактчеку:** `go-runtime`, `symfony`, `go-http`, далі `go-db`, `php-orm` і повторна перевірка `sql`;
    - **не почато:** `composer-psr`, `phpunit`.

    Перед продовженням звір файли з цим списком: агенти могли дописати після зупинки. `content-lint --all` покаже стан. Знімки до хвилі 3 (образи, контейнери, старі теми для довжин) лежать у scratchpad сесії: `wave3/`, `lengths-before/`.
- **Вирівнювання довжин (хвіст 2026-11-17).** Новий `content-lint --all` показує помилки довжин у темах, написаних до правила або до нового пункту «хоча б в 1 з 9»:
  - `csharp` — 4 питання;
  - `ef-core` — 9 питань і найдовша в 7 з 8;
  - `sql` — 7 питань і найдовша в 5 з 8;
  - `dotnet-async` — 2 питання;
  - `dotnet-clr` — 1 питання;
  - `architecture`, `spring-boot` — найдовша в 0 з 9.

  Правити лише довжини, не факти. План: окрема «тема» для одного автора хвилі 3. `python-core` і `django` повертаються авторам хвилі 2 до фактчеку. `backend/.claude/tools/option-lengths.py` видалено 2026-11-18: його замінив `content-lint.py`.
- **Backend завершено (2026-11-18).** `composer-psr` — PASS з 1 кола, `phpunit` — PASS з 2 кола (`phpunit-012` тепер на штатному `withParameterSetsInOrder()`, перевірено запуском). Повний прогін на знімку: 504 питання в 42 темах, `content-lint --all` OK, `import` +49 / ~18 / деактивовано 1 (`php-lang-010`), `dotnet test` 309 / 0. Docker після хвилі 3: нових образів і контейнерів немає.
- **Frontend — конвеєр (з 2026-11-18, власник: «далі фронтенд хвилі, в першу чергу Angular, React, Vue»).** Версії — у брифі. Черга (стеки переплетено, щоб паралельні автори не перетинались):
  1. `angular-components`, `react-core`, `vue-core`;
  2. `angular-change-detection`, `react-hooks`, `vue-composition-api`;
  3. `angular-di`, `react-state`, `pinia`;
  4. `rxjs`, `react-data-routing`, `vue-router`;
  5. `angular-routing-forms`, `nextjs`, `nuxt`;
  6. `ngrx`, потім `html-css`, `web-performance`, `frontend-testing`.
- **Frontend, прогрес:** `vue-core` — PASS з 1 кола; `react-core` — PASS з 2 кола (у 001 дистрактор «масив без `key`» був захищуваним, у 008 — двозначне формулювання).
  - `angular-components`, `vue-composition-api` — PASS з 1 кола (автори вносять уточнення: `afterEveryRender` замість `afterRender` в поясненні 010, формулювання 005 тощо).
  - `react-hooks` — PASS з 1 кола. **`pinia` — FAIL, коло 1, `critical` (2026-11-18):** у `pinia-003` дистрактор «`state.count` у стрілці» працює, бо Pinia 4.1 передає getter-у весь стор (`call(store, store)`); `pinia-006` залежить від dev/production (`$reset` у production — noop); `pinia-012` — неточна механіка. Повернуто автору. **За правилом «зупинка на `critical`» нові теми не запускаються до слова власника.** У роботі на момент зупинки: автори `angular-di`, `nextjs`; tester `angular-components`/`vue-composition-api`, `react-state`/`angular-change-detection`, `vue-router`. Чекають фактчеку: `react-data-routing`. Не почато: `rxjs`, `angular-routing-forms`, `ngrx`, `nuxt`, `html-css`, `web-performance`, `frontend-testing`.
- **Хвилі 4–5 (Frontend, початковий план):** `html-css`, `react-core`, `react-hooks`, `react-state`, `nextjs`, `react-data-routing`, `frontend-testing`, `web-performance`, `angular-components`, `angular-di`, `angular-change-detection`, `rxjs`, `ngrx`, `angular-routing-forms`, `vue-core`, `vue-composition-api`, `pinia`, `vue-router`, `nuxt`.
- **Хвилі перекладу (ADR 006; після PRP-030; бриф `backend/.claude/pm/translation-brief.md`).**
  - **Конвеєр** той самий, що в контенті:
    - один запуск `content-author` (Sonnet) — одна тема;
    - `tester` (Opus) бере 2–4 теми одного стеку;
    - слоти — до 5 агентів. Tester перевіряє 100 % питань, але без документації, тож співвідношення за вузьким місцем визначає головна сесія.
  - **Перед хвилею:** `cp -R ../content <scratchpad>/translation-base`. Лінт автора й tester іде з `--base <копія> --translation-only`.
  - **Доказ на знімку:** `Питання: додано 0, оновлено 0, деактивовано 0` і `Переклади питань: додано <питань у темах групи>`.
  - **Після PASS** головна сесія знімає `draft_en: true` з теми й позначає її тут.
  - **Тема з FAIL після 3 кіл** лишається з `draft_en: true`: у прод її англійська не піде, а правило 12 (PRP-031) її пропускає. Хвиля йде далі.
  - **Зупинка** — лише на `critical`, повтореному в другому колі тієї самої теми. Після останньої теми зупинки немає: далі PRP-031.
  - **Черга** (47 тем, 564 питання; стеки переплетено, щоб tester брав групи одного стеку):
    1. .NET: `csharp`, `dotnet-clr`, `dotnet-async`, `aspnet-core`, `ef-core` — калібрування: після перших 3 PASS один рядок власнику;
    2. спільні: `sql`, `architecture`, `messaging`, `docker-k8s`, `azure`, `aws`, `nosql`, `api-design`;
    3. Java: `java-core`, `jvm`, `java-concurrency`, `spring-boot`, `hibernate-jpa`;
    4. JS/Node: `javascript`, `typescript`, `nodejs-runtime`, `express-fastify`, `nestjs`, `prisma-typeorm`;
    5. Python: `python-core`, `python-async`, `django`, `fastapi`, `sqlalchemy`, `celery`, `pytest`;
    6. Go: `go-lang`, `go-concurrency`, `go-runtime`, `go-http`, `go-db`;
    7. PHP: `php-lang`, `laravel`, `symfony`, `php-orm`, `composer-psr`, `phpunit`;
    8. Frontend із PASS: `angular-components`, `react-core`, `react-hooks`, `vue-core`, `vue-composition-api`.
  - **Не перекладаємо:** 14 тем із `draft: true`. Їх напишуть двома мовами, коли відновляться хвилі Frontend.
  - **Прогрес:** —
- **Прогрес** (тема → питань, статус): `csharp` 12, `ef-core` 12, `sql` 12 (seed); `dotnet-async` 12, `dotnet-clr` 12, `aspnet-core` 12 (PRP-018, PASS); хвиля 1 (PASS): `azure`, `docker-k8s`, `architecture`, `messaging`, `java-core`, `jvm`, `java-concurrency`, `spring-boot`, `hibernate-jpa`, `aws`, `nosql`, `api-design` — по 12. Хвиля 2 (зупинена ENOSPC 2026-11-15): `python-core`, `javascript`, `django` — по 12, без запуску коду й фактчеку; решта 8 тем не почато.

## Дорожня карта

- **Етап 1 · Бекенд і контент MVP** (репо `Prepround`) — **виконано 2026-11-14**: PRP-002 → PRP-003 → PRP-014 → PRP-004 → PRP-005 → PRP-006, усі з першого кола, 271 тест, failed 0.
  - Результат: `import` наповнює БД каталогом IT і 36 питаннями, `GET /api/catalog` і `POST /api/tests` працюють за spec.
  - **Зупинка: чекліст для власника** (документ `/openapi/v1.json` можна відкрити в Postman). БД `prepround-postgres:5433` уже мігрована й наповнена (coder у PRP-005), а user-secrets ще порожні (`dotnet user-secrets list` → «No secrets configured for this application.»). Без них API на `/api/catalog` і `/api/tests` відповідає `500`, хоча `/api/health` працює. Усе з `/Users/dmitryspain/RiderProjects/Prepround/backend`:
    1. `docker compose up -d` (контейнер уже запущений, повтор безпечний).
    2. Один раз: `dotnet user-secrets set "ConnectionStrings:Prepround" "Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=prepround" --project src/Prepround.Api` (README, крок 3).
    3. `dotnet run --project src/Prepround.ContentTool -- import ../content` → `0`, у кожному рядку підсумку «додано 0, оновлено 0, деактивовано 0» (дані вже імпортовано; заодно це перевірка, що user-secrets читаються).
    4. В окремому терміналі: `dotnet run --project src/Prepround.Api --launch-profile https`.
    5. `curl -sk https://localhost:7120/api/catalog` → 1 галузь, 3 рівні, 9 стеків, 61 тема; у `ef-core` `questionCounts` дорівнює `{"junior":4,"middle":4,"senior":4}`.
    6. `curl -sk -X POST https://localhost:7120/api/tests -H 'Content-Type: application/json' -d '{"fieldId":"it","levelIds":["middle","senior"],"topics":[{"topicId":"ef-core","count":5},{"topicId":"sql","count":5}],"excludeQuestionIds":[]}'` → 10 питань із поясненнями, варіанти перемішані. З `"count":50` → `400` і `Test.NotEnoughQuestions` з назвою теми.
    7. Зворотний зв'язок (що незручно в API, що хибно в питаннях) власник передає PM.
- **Етап 2 · Веб MVP** (репо `PreproundWeb`): PRP-007 → PRP-009 → PRP-010 → PRP-011 — **виконано 2026-11-14**, усі з першого кола, 137 тестів фронту. Власник пройшов MVP у браузері, конкретних багів не назвав, а відгук — «дуже просте, хочу красоту» (етап 2a).
  - Результат: у браузері на `http://localhost:4300` можна налаштувати тест по стеку .NET, пройти його з таймером і «Перевірити», а потім побачити відсоток, слабкі теми й пояснення до помилок.
  - Межа MVP через seed: питання є лише в `csharp`, `ef-core` і `sql` (по 12, по 4 на рівень). У тесті максимум 36 питань, з одним рівнем — 12 (див. knowledge).
  - **Зупинка: чекліст запуску для власника.** Довірений dev-сертифікат уже є (PRP-001). Рядок підключення в user-secrets задається один раз на етапі 1, команда — у `Prepround/README.md`.
    1. `open -a Docker`, дочекатися `docker info`.
    2. `cd /Users/dmitryspain/RiderProjects/Prepround/backend && docker compose up -d`. Якщо `dotnet user-secrets list --project src/Prepround.Api` пише «No secrets configured…», спершу крок 2 чекліста етапу 1: інакше API віддає `500` на каталог, і фронт покаже помилку сервера.
    3. `dotnet tool restore && dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api` → `0`.
    4. `dotnet run --project src/Prepround.ContentTool -- import ../content` → `0` (повторний запуск безпечний).
    5. В окремому терміналі: `dotnet run --project src/Prepround.Api --launch-profile https`. Перевірка: `curl -sk https://localhost:7120/api/health` → `{"status":"ok"}`.
    6. В іншому терміналі: `cd /Users/dmitryspain/RiderProjects/PreproundWeb && npm install && npm start`.
    7. Відкрити `http://localhost:4300` у Chrome або Safari. Backend → .NET (C#), усі рівні, 20 питань. Очікувано: 7/7/6 на C#, EF Core і SQL, решта 7 тем з «0 питань», «Почати» активна.
    8. Пройти тест: на кількох питаннях натиснути «Перевірити» (відповідь блокується, видно пояснення), кілька лишити без відповіді, «Завершити тест» → підтвердження з кількістю.
    9. Результат: відсоток, «X з Y», час, розбивка по 3 темах від найслабшої, «Лише помилки», «Пройти ще раз».
    10. Додатково: один рівень і 12 питань (4/4/4); 10 питань із вимкненим таймером.

    Якщо на сторінці «Не вдалося зв'язатися із сервером.», перевірити, що API запущено (крок 5). Якщо «Сервер не зміг обробити запит. Спробуйте пізніше.», найімовірніше, немає рядка підключення (крок 2). Зворотний зв'язок (що незручно, що хибно в питаннях) власник передає PM: з нього вийдуть картки перед етапом 3.
- **Етап 2a · Оформлення** (репо `PreproundWeb`, ідея власника 2026-11-14): PRP-015 → PRP-016.
  - Результат: три теми з характером (Aurora, Daylight, Terminal) і «Авто» за системною темою, власний колір акценту, кругова анімація перемикання, сторінка «Налаштування» з прев'ю тем; екрани `/`, `/test`, `/result` і шапка на спільній дизайн-системі, як в Outlay. Взірець — `/Users/dmitryspain/RiderProjects/OutlayUI-latest` (лише читати).
  - **Зупинка: чекліст для власника** (API і dev server уже запущені головною сесією; якщо ні — кроки 1–6 чекліста етапу 2):
    1. `http://localhost:4300/settings` у Chrome: вибрати кожну тему (нова тема розкривається колом від місця кліку), кілька кольорів акценту й повзунок.
    2. Перезавантажити сторінку: вибір на місці, без спалаху іншої теми.
    3. «Авто»: перемкнути тему macOS (System Settings → Appearance) — сторінка сама переходить між Aurora і Daylight.
    4. Пройти тест .NET на 10 питань в улюбленій темі: на ширині телефона (DevTools, 390 px) і на десктопі; під час тесту в шапці немає посилань.
    5. На `/result` слабкі теми підсвічені червоним або жовтим.
    6. Відгук (що не подобається в темах, що незручно) — PM: з нього вийдуть правки перед PRP-012 або «Ідеї».
- **Етап 2b · Історія і «вже бачені»** (репо `PreproundWeb`): PRP-012.
  - Результат: результати зберігаються на пристрої, є екран «Історія», а наступний тест віддає перевагу питанням, яких людина ще не бачила.
  - Коротка зупинка: власник перевіряє історію. Потім PM ставить картки етапу 3.
- **Етап 3 · Контент** (репо `Prepround`; ідея власника 2026-11-14 «гарні питання різної складності»; **ADR 004 proposed**): PRP-017 → PRP-018 → зупинка калібрування → PRP-019 → далі.
  - Результат: кожна з 10 тем стеку .NET має питання на всіх рівнях, кожне питання має посилання на офіційну документацію, яке підтверджує відповідь, а рівні відповідають рубриці ADR 004.
  - Джерела, рубрика, поле `sources`, чекліст якості, версії (.NET 10 / C# 14, PostgreSQL 18) і агент `content-author` — ADR 004. Stack Overflow, GitHub issues і чужі набори питань не використовуємо (там таблиця з ліцензіями).
  - **План:** спершу ширина, потім глибина (ADR 004, розділ 7). Картки до ~40 питань (правило 16).
    1. PRP-017 — формат `sources` і джерела для 36 seed (coder).
    2. PRP-018 — `dotnet-async`, `dotnet-clr`, `aspnet-core` по 12 (content-author).
    3. **Зупинка калібрування:** власник читає 10 випадкових питань PRP-018 («так би спитали?», «рівень вгадано?»). Відгук правимо в ADR 004 або в агенті до масового наповнення.
    4. PRP-019 — `architecture`, `messaging`, `docker-k8s` по 12.
    5. Далі (картки ставить PM після PRP-019): `azure` 12; потім глибина до 30 на тему (10/10/10) для всіх 10 тем — ~6 карток по 2 теми × 18 питань.
  - Наприкінці — зупинка: власник проходить тест .NET на 30–50 питань з усіма рівнями. Потім — інші стеки (кожен починається рядком бази версій в ADR 004, розділ 5).
- **Етап 4a · Деплой MVP на безкоштовний хостинг** (рішення власника 2026-11-19, **ADR 005 accepted**). Порядок: PRP-020 (власник, паралельно з coder) → PRP-021 → PRP-023 → PRP-022 → PRP-024.
  - **Результат:** людина з будь-якого пристрою відкриває `https://prepround.pages.dev`, налаштовує й проходить тест. API на Render Free (Frankfurt) читає лише перевірені питання з Neon (Frankfurt) через роль лише для читання. Деплой — push у `master`.
  - **Зупинка:** власник проходить тест з телефона й десктопа, метрики «до/після» записано в ADR 005.
  - **Розкладено вхідні дані етапу 4 (PM, 2026-11-19):**
    - `ForwardedHeaders` → PRP-021: `CF-Connecting-IP`, а не найправіший `X-Forwarded-For`, бо за Render стоїть Cloudflare (ADR 005, розділ 4);
    - `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` → PRP-021: образ `aspnet:10.0` на Ubuntu з ICU, критерій «порядок тем у контейнері дорівнює локальному»;
    - `import` один за раз → PRP-024 і зміна процесу: імпорт у прод запускає лише власник;
    - `ASPNETCORE_ENVIRONMENT=Production` → PRP-021 (у Dockerfile, критерій `404` на `/openapi/v1.json`);
    - `Request.Invalid` лишається в «Ідеях» (P2): типізований фронт такого тіла не шле;
    - CI/CD, сканування образів, домен → «Ідеї»;
    - бекапи не потрібні: БД відтворюється з git (міграції + `import`, ADR 005, розділ 5).
- **Етап 4 · Деплой на prepround.app** (почати з ADR): хостинг, CI/CD для двох репо, Docker, сканування образів, бекапи. Для власника це DevOps-кейс у портфоліо, тож фіксувати метрики «до/після». *(2026-11-19: хостинг і Docker — етап 4a вище; решта — «Ідеї».)*
  - **Вхідні дані для ADR деплою (хвости етапу 1):**
    - **`ForwardedHeaders`** (PRP-006, coder і tester): rate limiter `tests` ділить клієнтів за `RemoteIpAddress`. За проксі всі клієнти потраплять в один розділ і разом отримають `429` після 30 запитів на хвилину;
    - **`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`** (PRP-005, tester): теми сортуються `StringComparer.InvariantCulture`. В офіційних alpine-образах .NET invariant-режим увімкнено за замовчуванням, і тоді порівняння стає ordinal: `AWS` перед `Async…`, кирилиця й нижній регістр після великої латиниці. Або образ з ICU і `false`, або явне рішення про компаратор. Тест tester на такому середовищі впаде, і це правильно;
    - **`import` один за раз** (PRP-004, tester): два одночасні `import` можуть зіткнутися на PK (`GetStatusAsync` і `LoadAsync` поза транзакцією запису), тоді один отримає код 3. Даних це не псує, але в CI імпорт має йти послідовно;
    - **перед публічним запуском:** `Request.Invalid` замість ProblemDetails (див. «Ідеї»); developer exception page з stack trace на `413` є лише в Development, тож перевірити, що прод іде з `ASPNETCORE_ENVIRONMENT=Production`.
- **Етап 5 · Англійська версія** (рішення власника 2026-11-19, **ADR 006 accepted**). Порядок: PRP-025 → PRP-026 → PRP-027 → PRP-028 → PRP-029 → PRP-030 → хвилі перекладу → PRP-031 → PRP-032.
  - **Результат:** людина перемикає сайт на англійську в шапці або на «Налаштуваннях». Інтерфейс, назви тем і питання 47 опублікованих тем показуються англійською, без перезавантаження. Тест англійською бере лише перевірені переклади, а українська версія не змінилась.
  - **Зупинка:** прод-імпорт перекладу, push фронту з PRP-031, власник проходить тест англійською з телефона й десктопа (PRP-032).
- **Відкладено без дати:** iOS (PRP-008 і колишній етап «iPhone власника»), у `PreproundWeb/ios`. Див. розділ «Відкладено: iOS».

## Ідеї (ще не картки)

- **Після деплою (ADR 005, 2026-11-19):**
  - **Домен prepround.app.** На 2026-11-13 вільний, не куплений; він платний, тож лише за рішенням власника. Потрібні custom domain на Pages і Render, а origin домену — в `appsettings.Production.json`.
  - **Keep-alive пінг** `/api/health` раз на ≤ 14 хв (рішення власника). 750 год покривають один сервіс 24/7. Health БД не чіпає, тож Neon однаково засинає. Без пінгу людина чекає до ~1 хв на першому запиті (PRP-022 це пояснює).
  - **CI (GitHub Actions):** `dotnet test` і `npx ng test --watch=false` на кожен push; у Render — «Auto-Deploy: After CI checks pass». Сюди ж:
    - тижнева перевірка живості `sources` (див. нижче);
    - сканування образу (Trivy);
    - pin базових образів за digest плюс Renovate або Dependabot: Render кешує плаваючі теги.
  - **Автоімпорт контенту** після PASS через GitHub Action із секретом ролі-власника. Компроміс: секрет живе в GitHub, а не лише в терміналі власника.
  - **CSP і заголовки безпеки** (P2): `public/_headers` на Pages з `script-src 'self'`. `theme-init.js` уже окремий файл, але Angular `inlineCritical` додає inline `onload`, тож потрібен `security.autoCsp` або вимкнення `inlineCritical`. На API — `X-Content-Type-Options: nosniff`.
  - **Стиснення відповідей API** (Brotli/gzip), якщо трафік Render дійде до 50 % від 5 ГБ на місяць. Зараз: каталог 13,7 КБ, 20 питань — 23,4 КБ без стиснення.
  - **`EnableRetryOnFailure` в Npgsql**, якщо на проді з'являться поодинокі `500` після сну Neon. Зараз від цього береже `Connection Idle Lifetime=60` у рядку Render.
  - **Rate limit для IPv6** — ділити за `/64`, а не за адресою. Інакше клієнт з IPv6 обходить ліміт зміною адреси в межах своєї мережі.
  - **Workers Static Assets** замість Pages, якщо Cloudflare почне обмежувати Pages (ADR 005, «Ризики»).
- Акаунти й синхронізація історії між пристроями (ADR).
- Адмінка для питань, коли з'являться автори без git (рішення 002).
- Нові напрями IT: DevOps, QA, Mobile, Data. Потім інші галузі.
- ~~Англійська версія інтерфейсу й контенту.~~ → ADR 006, етап 5.
- **Після етапу 5 (ADR 006):**
  - **Пошуковики для англійської:** окремі URL на мову, `hreflang`, prerender. Зараз SPA без SSR має одну адресу, тож англійська версія окремо не індексується;
  - **Глосарій термінів перекладу** в `translation-brief.md` — якщо tester двічі поверне той самий термін;
  - **Третя мова:** рядок у `ContentLanguages`, словник `core/i18n/messages/<мова>.ts`, хвиля перекладу. Якщо мов стане більше трьох, перейти на значення-словники `text: {uk, en, …}` (ADR 006, «Відкинуто»);
  - **`<title>` і `meta description` мовою інтерфейсу.**
- Підсвітка синтаксису в блоках коду.
- Збереження незавершеного тесту.
- Прогрес по темах у часі («SQL: 40% → 75% за тиждень»).
- Розбір питань у `/history/:id`. Історія зберігає лише id і вибрані варіанти (spec 3.5), тож для розбору потрібен або ендпоінт за id питань (зміна контракту), або знімки питань у сховищі (а `localStorage` обмежений кількома МБ).
- «Пройти ще раз» з історії й «Нові налаштування» з підставленими попередніми значеннями.
- ~~Агент `content-author` для пачок питань з обов'язковими джерелами.~~ → ADR 004 (proposed), розділ 6.
- **«Детальніше» в розборі** (ADR 004, розділ 3), P2, після етапу 3. Показувати посилання з `sources` під поясненням на `/result`: людина, що помилилась, одразу бачить, що почитати. Потрібні колонка в БД (міграція), поле в `import`, `sources: string[]` у spec 5.2 і в `PreproundWeb` (`models.ts`, `api.service.ts`, `ResultView`). Дані вже будуть у YAML після PRP-017. Відкривати лише через `<a rel="noopener noreferrer" target="_blank">` з URL, що починається з `https://`.
- **Перевірка живості `sources` у CI** (етап 4): той самий `curl`-цикл, що в критеріях PRP-017, раз на тиждень, а не на кожен коміт (зовнішні сайти бувають недоступні).
- Генерація TS-моделей у `PreproundWeb` з `/openapi/v1.json` (рішення 003, «Відкинуто»): варто, якщо контракт між двома репо почне розходитись. Потрібні npm-пакет і ADR.
- Коли власник створить GitHub-ремоути: посилання між README обох репо.
- OpenAPI: ендпоінти описують, крім `application/json`, ще `text/plain` і `text/json` (зауваження 9 tester, PRP-002). Косметика, P3. Стане важливим разом з ідеєю генерації TS-моделей: тоді лишити лише `application/json`.
- **Правило «каркас галузі непорожній»** (зауваження 5 tester, PRP-003): порожні `levels`, `directions`, `stacks` напряму чи `topics` стеку зараз не помилка, якщо в галузі немає питань. Без рівнів чи тем тест у такій галузі неможливий, але єдина галузь `it` має все. Поставити карткою (P2) перед першою новою галуззю, разом із правкою правил у рішенні 002.
- Повідомлення читача для `correct: [true]`: зараз «має бути текстом», краще «має бути true або false» (зауваження 4 tester, PRP-003). Косметика для автора, P3.
- **Перерозподіл при зміні рівнів** (PRP-009, tester 1 і 2), P2. Звузили рівні до одного, потім повернули всі — значення лишаються 4/4/4, «Розподілено 12 з 20», і людина виправляє вручну. А тема, що знову стала активною, запускає повний перерозподіл і затирає ручні значення. Spec 3.1, п. 5 перерозподіляє лише при зміні набору тем або загальної кількості. Пропозиція до spec: перерозподіляти порівну й при зміні рівнів (просто й передбачувано). Потрібне рішення власника, тоді — картка.
- **Дрібний борг фронту** — зібрати в одну картку P3, коли буде привід торкатися цих файлів:
  - `PreproundWeb/.vscode/launch.json` відкриває `http://localhost:4200/`, а dev server на 4300 (PRP-007, tester 1). Власник працює в Rider, тож не заважає;
  - дробова кількість питань (`12.5`) показує «Від 10 до 100 питань.», хоча число в межах; «Почати» блокується правильно (PRP-009, tester 4). Краще «Ціле число від 10 до 100.»;
  - `npx prettier --check` не проходить на `src/app/app.config.ts` і `setup.spec.ts` (кінцеві коми; PRP-011, coder);
  - рядок 22 `Prepround/README.md` («The frontend (Angular) lives in a separate repository…») дублює розділ Frontend (PRP-007, coder і tester 4).
- **Router View Transitions** (`withViewTransitions()`) між сторінками: плавні переходи `/` → `/test` → `/result` у стилі PRP-015. Після відгуку власника на етапі 2a.
- **`Request.Invalid` для синтаксично некоректного тіла** (PRP-006, coder і tester 2), P2, до етапу 4. Зараз `"count":"x"`, `"levelIds":"middle"` чи `null` дають `400` ProblemDetails від `[ApiController]` з внутрішнім ім'ям типу (`Prepround.Application.TestGeneration.TestTopicRequest`), а не `{code, message}` зі spec 5. Варіант — `InvalidModelStateResponseFactory` з кодом `Request.Invalid`. Типізований фронт таке тіло не надсилає, а PRP-009 показує загальний текст для `400` без `message`, тож у MVP нічого не ламається. Разом із цим вирішити, чи описувати `500` у spec 5 (зараз без БД `GET /api/catalog` дає `500`, PRP-005, tester 3).
- **Дрібний борг бекенду** — зібрати в одну картку P3, коли буде привід торкатися цих файлів:
  - `Test.TooManyExcluded` перевіряти першим, до звернень до БД: зараз запит із понад 10 000 id коштує два запити до БД (PRP-006, tester 1);
  - `Retry-After` у відповіді `429` (PRP-006, coder і tester 4). Spec цього не вимагає, а фронт і так пише «Спробуйте за хвилину»;
  - тест ліміту тіла 1 МБ через `factory.UseKestrel()`: у TestServer немає `IHttpMaxRequestBodySizeFeature`, тож ліміт перевірено лише вручну curl-ом (PRP-006, tester 3);
  - `CatalogController` не `sealed` (PRP-005, tester 4);
  - захист від циклу симлінків у `ReportUnreadYaml` (`YamlContentReader.cs`): цикл дасть переповнення стека, а не помилку. У git-контенті малоймовірно (PRP-014, tester 2);
  - підказка «це тека галузі, а не корінь контенту», якщо `validate` / `import` отримали теку з `field.yaml` у корені: зараз ~63 помилки «не буде прочитано» замість однієї (PRP-014, tester 3).

---

## Картки

### PRP-001 · Оновити Node, довірити свіжий dev-сертифікат, оновити .NET SDK
- **Статус:** done (2026-11-14)
- **Виконавець:** власник
- **Пріоритет:** P1
- **Область:** frontend + backend
- **Залежить від:** —
- **Навіщо:**
  - Angular 22 вимагає Node `^22.22.3 || ^24.15.0 || >=26`, а встановлено 22.13.1. Без оновлення каркас фронту (PRP-007) не створити.
  - Фронт на `http://localhost:4300` ходить на API `https://localhost:7120`. Запит до сервера з недовіреним сертифікатом браузер відкидає, тож без пункту 3 веб-MVP бачитиме API недоступним.
  - SDK 10.0.100 / runtime 10.0.0 відстали на 15 патчів безпеки (не блокує).
- **Що зробити:**
  1. **Обов'язково, блокує PRP-007. Node 22.23.5 LTS.** Node встановлено з офіційного pkg, тож оновлюємо так само: https://nodejs.org/dist/v22.23.5/node-v22.23.5.pkg. Лишаємося на гілці 22, щоб не зламати Angular 17 в Outlay (рішення 001).
  2. **Бажано, не блокує. .NET SDK 10.0.404 і runtime 10.0.15** (`global.json` прийме і 10.0.100): https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-osx-arm64.pkg
  3. **Обов'язково до 2026-11-25, блокує перевірку веб-MVP.** Новий довірений dev-сертифікат: `dotnet dev-certs https --clean && dotnet dev-certs https --trust`.
  4. За бажанням: `sudo chown -R 501:20 ~/.npm`.
- **Критерії приймання:**
  - [x] `node -v` → `v22.23.5` (головна сесія, 2026-11-14)
  - [x] `dotnet dev-certs https --check --trust; echo $?` → `0`; сертифікат `CN=localhost` `A3A6F19E…21D2`, діє з 2026-11-14 00:19:39Z до **2027-11-14 00:19:39Z**, тобто пізніше за 2026-11-25 (PM перевірив, 2026-11-14)
  - [ ] (пункт 2, необов'язковий) `dotnet --list-sdks` містить `10.0.404` — ні, досі лише `10.0.100`
- **Результат:** обов'язкові пункти 1 і 3 виконано, PRP-007 і перевірка MVP більше не блоковані цією карткою. Закрито як `done`. Необов'язкові пункти 2 (.NET SDK) і 4 (npm-кеш) перенесено в PRP-013. Пункт 4 став обов'язковим до PRP-007: у `~/.npm` 11 256 файлів root, серед них 588 тек `_cacache/index-v5`, і це вже ламає npm: `npx @angular/cli@22.2.7 new … --dry-run` → exit 243, `EACCES` у `~/.npm/_cacache` (PM, 2026-11-14).

### PRP-002 · Каркас бекенду
- **Статус:** done (2026-11-14, PASS з першого кола)
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** рішення 001
- **Навіщо:** основа для всіх бекенд-карток: solution, шари, конвенції, Postgres у Docker, health.
- **Що зробити** (усе в `backend/`, якщо не сказано інше):
  1. **`global.json`:** `{"sdk": {"version": "10.0.100", "rollForward": "latestFeature", "allowPrerelease": false}}`.
  2. **`Directory.Build.props`:** `TargetFramework` net10.0, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true. **`Directory.Packages.props`** — центральне керування версіями пакетів.
  3. **`Prepround.slnx`** з проєктами:
     - `src/Prepround.Domain` (classlib);
     - `src/Prepround.Application` (classlib → Domain);
     - `src/Prepround.Infrastructure` (classlib → Application);
     - `src/Prepround.Api` (web → Application, Infrastructure);
     - `src/Prepround.ContentTool` (console → Application, Infrastructure);
     - `tests/Prepround.Tests` (xUnit v3 → усі).
  4. **Domain:** `Error` (record: `Code`, `Message`, `Error.None`), `Result` (`IsSuccess`, `IsFailure`, `Error`, `Success()`, `Failure(error)`) і `Result<T>` (`Value`, який кидає `InvalidOperationException` при доступі в Failure, та implicit conversion з `T`).
  5. **Application:**
     - `Abstractions/Messaging`: `IQuery<TResponse>`, `IQueryHandler<TQuery, TResponse>`, `ICommand<TResponse>`, `ICommandHandler<TCommand, TResponse>`. Хендлер повертає `Task<Result<TResponse>>` і приймає `CancellationToken`;
     - `DependencyInjection.AddApplication()` реєструє всі хендлери збірки рефлексією, без Scrutor.
  6. **Infrastructure:** порожній `PreproundContext` (без сутностей і без міграцій) і `AddInfrastructure(IConfiguration)`, який підключає Npgsql з `ConnectionStrings:Prepround`. Якщо рядка підключення немає, API все одно стартує: контекст створюється лише при першому використанні.
  7. **Api:**
     - контролери; `AddOpenApi` + `MapOpenApi` лише в Development;
     - CORS-політика з `Cors:Origins`. В `appsettings.json`: `["http://localhost:4300"]`. Origins беруться лише з конфігурації, без хардкоду в коді;
     - `HealthController`: `GET /api/health` → `200 {"status":"ok"}`, без звернень до БД (spec 5.3);
     - розширення для відповіді помилкою: `BadRequest(new { code, message })`;
     - `launchSettings.json`: профіль `https` на `https://localhost:7120;http://localhost:5120`, профіль `http` на `http://localhost:5120`;
     - `UserSecretsId` — справжній GUID (в Outlay був буквальний `{GUID}`).
  8. **ContentTool:** без аргументів або з невідомою командою друкує usage українською й виходить з кодом 2. Команди `validate` і `import` з'являться в PRP-003 і PRP-004. `UserSecretsId` той самий, що в Api, щоб рядок підключення задавався один раз.
  9. **`docker-compose.yml`:**
     - сервіс `postgres`, образ `postgres:18-alpine`, `container_name: prepround-postgres`, порт `5433:5432`, том `prepround_pgdata`, `restart: unless-stopped`;
     - healthcheck через `pg_isready`;
     - `POSTGRES_DB/USER/PASSWORD = prepround`. Це dev-пароль лише для локального контейнера;
     - рядок підключення не потрапляє в `appsettings*.json`, у README є команда `dotnet user-secrets set "ConnectionStrings:Prepround" "Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=prepround" --project src/Prepround.Api`.
  10. **`.config/dotnet-tools.json`** з `dotnet-ef` 10.0.15. **`.gitignore`** через `dotnet new gitignore`.
  11. **Тести:** проєкт `tests/Prepround.Tests` з одним smoke-тестом: `GET /api/health` через `WebApplicationFactory` → 200. Тут coder пише тест, бо цього вимагає картка каркаса.
  12. **`README.md` у корені репо, англійською:** що це за продукт, структура монорепо, як запустити бекенд (Docker, user-secrets, `dotnet run`) і як довірити dev-сертифікат (`dotnet dev-certs https --trust`), бо фронт ходить на `https://localhost:7120`. Розділи для фронту й контенту лишити заглушками «coming soon».
- **Дозволені пакети:**
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3;
  - Microsoft.EntityFrameworkCore.Design 10.0.15 (`PrivateAssets=all`);
  - Microsoft.AspNetCore.OpenApi 10.0.15;
  - Microsoft.Extensions.Configuration.UserSecrets 10.0.15 (для ContentTool);
  - xunit.v3 4.0.3, xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.12.0, Microsoft.AspNetCore.Mvc.Testing 10.0.15.

  MediatR, Autofac, Swashbuckle і мок-бібліотеки заборонені.
- **Критерії приймання:**
  - [x] `cd backend && dotnet build; echo $?` → `0`, у виводі `0 Warning(s)`
  - [x] `dotnet test; echo $?` → `0`, 1 тест passed (у coder; після tester — 28 passed, 0 failed)
  - [x] `dotnet tool restore && dotnet ef --version` → містить `10.0.15`
  - [x] API запущено **без** Postgres: `curl -sk https://localhost:7120/api/health` → HTTP 200, тіло `{"status":"ok"}`
  - [x] `curl -sk -i -X OPTIONS https://localhost:7120/api/health -H "Origin: http://localhost:4300" -H "Access-Control-Request-Method: GET"` → є `Access-Control-Allow-Origin: http://localhost:4300`. Той самий запит з `Origin: https://evil.example` → заголовка `Access-Control-Allow-Origin` немає
  - [x] `curl -sk -o /dev/null -w '%{http_code}' https://localhost:7120/openapi/v1.json` → `200` (Development)
  - [x] `docker compose up -d`, потім `docker exec prepround-postgres pg_isready; echo $?` → `0`. `docker port prepround-postgres` → `5432/tcp -> 0.0.0.0:5433`
  - [x] `dotnet run --project src/Prepround.ContentTool; echo $?` → usage і `2`
  - [x] `grep -rniE "password|Host=" backend --include="appsettings*.json"` → порожньо
  - [x] `UserSecretsId` в Api і ContentTool — однаковий валідний GUID
- **Для tester:**
  - `TreatWarningsAsErrors` справді діє: тимчасове попередження ламає збірку;
  - API стартує без рядка підключення і без БД;
  - порти не перетинаються з Outlay (7016, 5432);
  - `capacitor://localhost` у CORS **не потрібен**: iOS відкладено рішенням власника 2026-11-14 (PRP-008). Пункт про нього в `tester.md` застарів, і його відсутність не є FAIL.
- **Контракт API:** новий `GET /api/health` (spec 5.3).
- **Поза скоупом:** сутності, міграції, контент, rate limiting, фронт, `capacitor://localhost` у CORS (відкладено, PRP-008).
- **Результат (PM, 2026-11-14):** каркас `backend/` готовий: 6 проєктів, CPM, `Result`/`Error`, CQRS-інтерфейси з реєстрацією хендлерів рефлексією, лінивий Npgsql, CORS з конфігурації, OpenAPI лише в Development, `GET /api/health`, ContentTool з usage (exit 2), Postgres 18 на 5433, `README.md` у корені. Tester додав 27 тестів (усього 28, 12 мутацій зловлено), включно з архітектурним `LayerDependencyTests`.
  - **Відхилення від картки, прийняті:**
    - `global.json` має ще секцію `"test": {"runner": "Microsoft.Testing.Platform"}`. Без неї xunit.v3 4.0.3 на .NET 10 SDK не запускається через `dotnet test` (tester довів). Дозволено.
    - `backend/nuget.config` (лише nuget.org, `<clear/>` і package source mapping). На машині власника глобально є друге джерело (GraphQL-MyGet), і CPM з `TreatWarningsAsErrors` дає `NU1507` як помилку restore. Дозволено.
    - **`Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.15 в Application — дозволено заднім числом.** Без `IServiceCollection` неможливо написати `AddApplication()` (п. 5 картки), а це та сама DI з рішення 001. Пакет мав бути в «Дозволених пакетах»: це мій пропуск, а не самодіяльність coder. Записано в ADR 001 і knowledge.
    - Transitive pinning `Microsoft.EntityFrameworkCore` і `.Relational` на 10.0.15 у `Directory.Packages.props`, щоб Npgsql 10.0.3 не тягнув EF Core 10.0.4. Нових залежностей немає, версія з рішення 001.
    - Розширення називається `BadRequestError` (метод `ControllerBase.BadRequest` перекриває однойменне розширення). `BadRequest(result.Error)` дає той самий JSON `{code,message}`.
  - **Куди пішли зауваження:**
    - позначка «(ще не реалізовано)» в usage ContentTool → PRP-003 (`validate`) і PRP-004 (`import`);
    - тест, що хендлер резолвиться з DI (зауваження 7 tester) → PRP-004: там перший хендлер у проєкті (`ImportContentHandler`), а не в PRP-005. Тест загальний (`ValidateOnBuild`), тож PRP-005 і PRP-006 він покриває сам; у них — рядок «Для tester»;
    - `http://localhost:5120` під профілем `https` віддає 307, і preflight через редирект не проходить (зауваження 8) → PRP-007 (критерій і README/CLAUDE.md), PRP-009 («Для tester»), knowledge;
    - CORS уже має `AllowAnyMethod()` і `AllowAnyHeader()`, а `CorsTests` перевіряють preflight JSON `POST` → PRP-006, п. 4 переписано: політику не змінювати, лишається критерій `curl`;
    - `UseSetting(key, null)` у `WebApplicationFactory` дає `""` (зауваження 6) → knowledge і PRP-004 (порожній або відсутній рядок підключення — однаковий шлях, код 3);
    - критерій «1 тест passed» не відтворюється, бо tester додає тести (зауваження 4) → правило в knowledge: точну кількість тестів у критеріях не фіксую;
    - OpenAPI з `text/plain` і `text/json` (зауваження 9) → «Ідеї», P3;
    - runtime 10.0.0 / SDK 10.0.100 без патчів (зауваження 10) → PRP-013, пункт 3;
    - MTP-синтаксис `dotnet test` у «Командах» CLAUDE.md (зауваження 11) → уже внесено головною сесією;
    - EF Core 11.0.0 GA вийшов, Npgsql EF Core 11 — 11.0.0-rc.3 (перевірено в NuGet 2026-11-14) → примітка в ADR 001, рішення в силі;
    - дані про dev-сертифікат у knowledge уже актуальні (до 2027-11-14), правка не потрібна.

### PRP-003 · Формат контенту, каталог IT, seed-питання і `validate`
- **Статус:** done (2026-11-14, PASS з першого кола)
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** content + backend
- **Залежить від:** PRP-002, рішення 002
- **Навіщо:** контент — серце продукту. Формат і валідатор не дають зламаному YAML потрапити в базу.
- **Що зробити:**
  1. **Application, `Content/`:**
     - модель контенту — records `FieldDefinition`, `LevelDefinition`, `DirectionDefinition`, `StackDefinition`, `TopicDefinition`, `QuestionDefinition`, `OptionDefinition`, `CodeBlock`;
     - `ContentValidator` з усіма правилами 1–8 з рішення 002. Повертає список `ContentError(File, Id, Message)` українською, нічого не кидає.
  2. **Infrastructure, `Content/YamlContentReader`** (YamlDotNet):
     - читає `<dir>/<галузь>/field.yaml` і `<dir>/<галузь>/topics/*.yaml` для кожної папки галузі;
     - невідомий ключ (правило 9) і синтаксична помилка YAML стають `ContentError` з файлом і рядком, а не винятком.
  3. **ContentTool `validate <dir>`** (`src/Prepround.ContentTool/Program.cs`; зараз там лише usage з PRP-002):
     - є помилки → друкує кожну в stderr як `файл: id: повідомлення` і виходить з кодом 1;
     - помилок немає → друкує в stdout підсумок по кожній галузі (`it: рівнів 3, напрямів 2, стеків 9, тем 61, питань 36`) і виходить з кодом 0;
     - папки немає → повідомлення українською з назвою теки й код 2. `validate` без теки → usage і код 2;
     - **usage:** у PRP-002 обидві команди позначено «(ще не реалізовано)». Тепер ця позначка лишається тільки біля `import`. Виклик `import` до PRP-004 друкує «Команду import ще не реалізовано.» і usage, код 2 (а не «Невідома команда: import»). Без аргументів і з невідомою командою поведінка як у PRP-002 (usage, код 2).
  4. **`content/it/field.yaml`:** галузь `it` / `IT`, шкала `junior`/`middle`/`senior`, напрями й стеки з id, назвами й порядком тем **точно** за spec 4.1.
  5. **`content/it/topics/<id>.yaml`:** 61 файл, `id` / `title` / `description` за spec 4.2 (`description` = колонка «Покриває», одним реченням). Для тем без seed — `questions: []`.
  6. **Seed:** по 12 питань у `csharp`, `ef-core` і `sql`, разом 36.
     - У кожній темі 4 junior, 4 middle і 4 senior, щонайменше 3 `multiple` і щонайменше 1 з блоком `code`.
     - Id: `<тема>-001`…`<тема>-012`.
     - Питання мають бути реалістичними питаннями зі співбесід і **фактично правильними** за офіційною документацією (learn.microsoft.com, postgresql.org/docs). Пояснення: 1–3 речення про те, чому правильна відповідь правильна.
     - Діалект SQL: стандартний або PostgreSQL. Специфіку SQL Server не використовувати.
- **Дозволені пакети:** YamlDotNet 18.1.0 (лише Infrastructure; остання стабільна в NuGet на 2026-11-14). Версія вказується в `backend/Directory.Packages.props` (`PackageVersion`), а в csproj — `PackageReference` без `Version` (CPM з PRP-002). `backend/nuget.config` не змінювати.
- **Критерії приймання:**
  - [x] `cd backend && dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → підсумок `it: рівнів 3, напрямів 2, стеків 9, тем 61, питань 36` і `0`
  - [x] `validate` без теки → код `2`; `import ../content` → «Команду import ще не реалізовано.» і код `2`; usage без аргументів не містить позначки «(ще не реалізовано)» біля `validate`, а біля `import` містить
  - [x] `ls content/it/topics/*.yaml | wc -l` → `61`, а множина id збігається зі spec 4.2
  - [x] кожен стек у `field.yaml` має рівно 10 тем у порядку spec 4.1
  - [x] для кожного правила 1–9 рішення 002 є зламана копія контенту (у тимчасовій теці), на якій `validate` дає `1` і рядок з файлом і id
  - [x] `validate /nonexistent; echo $?` → `2`
  - [x] у `field.yaml` копії додано рівень `architect` і питання з `level: architect` → `validate` дає `0` без змін коду
  - [x] `grep -rniE "junior|middle|senior" backend/src` → порожньо
  - [x] `dotnet build` → 0 попереджень; `dotnet test; echo $?` → `0`, `failed: 0` (точну кількість тестів не фіксуємо)
- **Для tester:**
  - exit-коди перевіряй справжнім процесом ContentTool, як `tests/Prepround.Tests/ContentTool/UsageTests.cs`; ці тести мають лишитись зеленими (або оновлені під нову usage);
  - `LayerDependencyTests` лишаються зеленими: модель контенту й `ContentValidator` в Application не тягнуть YamlDotNet;
  - `corect: true` (друкарська помилка) → помилка, а не тиша;
  - однаковий id питання у двох файлах;
  - `multiple` з 1 правильним;
  - `single` з 2 правильними;
  - 1 і 9 варіантів;
  - ім'я файлу ≠ id;
  - тема зі стеку, якої немає;
  - порожній `explanation`;
  - вибірково звір **щонайменше 5 seed-питань** з документацією. Хибна правильна відповідь — `critical`.
- **Контракт API:** без змін.
- **Поза скоупом:** БД, імпорт, ендпоінти, решта 58 тем без питань.
- **Результат (PM, 2026-11-14):** формат контенту, каталог IT і `validate` готові.
  - Код: модель контенту й `ContentValidator` (правила 1–8) в Application; `YamlContentReader` в Infrastructure (YamlDotNet 18.1.0) обходить дерево вузлів, тож правило 9, синтаксис і неправильна форма значень стають `ContentError` з номером рядка, а не винятком; `ContentTool validate` з кодами 0 / 1 / 2. Тип результату читача `ContentReadResult` уже лежить в Application.
  - Контент: `content/it/field.yaml` (3 рівні, 2 напрями, 9 стеків по 10 тем, звірено зі spec 4.1), 61 файл тем (звірено зі spec 4.2), 36 seed-питань у `csharp`, `ef-core`, `sql`: 4/4/4 за рівнями, `multiple` 3/4/4, з блоком коду 5/3/3.
  - **Правильність seed:** tester прочитав усі 36. Coder і tester разом перевірили запуском (C#, Postgres 18, EF Core 10.0.15) 35 з 36, ef-core-003 — лише за документацією. Хибних правильних відповідей немає.
  - Tester: усього 152 тести, `failed: 0`, 12 мутацій зловлено; понад 40 ручних поломок, жодного необробленого винятку.
  - **Прийняті припущення coder** (основні записано в рішення 002, розділ «Уточнення реалізації»): порожній `title` — помилка; помилки читача вимикають правила 1–8 у цьому прогоні; корінь без галузей і тека без `field.yaml` — помилка (захист від імпорту не тієї теки); приховані файли й теки пропускаються; `correct` лише `true`/`false`; унікальність id окремо для рівнів, напрямів і стеків; `validate` з двома теками — код 2; у stderr після списку рядок `Помилок у контенті: N.`.
  - **Куди пішли зауваження:**
    - `.yml` / `.YAML` і YAML поза `topics/` читач пропускає мовчки (зауваження 1 tester, хвіст coder) → **PRP-014**, п. 3. PM відтворив: сьогодні перейменування чи перенесення наявної теми ловить правило 4 (`sql.yaml` → `sql.yml` дає exit 1 і «Теми «sql» немає»), бо всі 61 тема входять у стеки. Тиша лишається для теми поза стеками і для нового файлу, а після PRP-004 це означало б тиху деактивацію. Тому PRP-014 стоїть перед PRP-004: імпорт від першого дня не має цієї діри;
    - абстракція над `YamlContentReader` для хендлера імпорту (хвіст coder) → **PRP-014**, п. 1–2: `IContentReader` і один крок «прочитати й перевірити» в Application, ним користуються і `validate`, і (у PRP-004) імпорт. PRP-004 переписано під нього;
    - правильні варіанти в усіх 36 seed-питаннях стоять першими (зауваження 2) → **PRP-006**: окремий детермінований критерій перемішування варіантів через HTTP. **PRP-010**, «Для tester»: варіанти показуються в порядку відповіді API, без сортування за `key`;
    - помилки читача вимикають правила 1–8, тож автор може побачити помилки за два прогони (зауваження 3) → прийнято свідомо, бо інакше зламаний файл дає хибні «теми X немає» в кожному стеку. PRP-014 зберігає цю поведінку явно; рішення 002, «Уточнення»;
    - `correct: [true]` → «має бути текстом» (зауваження 4) → «Ідеї», P3;
    - порожня шкала `levels: []` без питань не помилка (зауваження 5, питання до PM) → **не зараз**: єдина галузь `it` має 3 рівні, а діра проявиться лише з новою галуззю. «Ідеї», правило «каркас галузі непорожній» — картка перед першою новою галуззю;
    - англійська деталь YamlDotNet у синтаксичних помилках (зауваження 6) → прийнято, knowledge;
    - exit 2 для `validate` у «Командах» CLAUDE.md → уже внесено головною сесією.
  - **Знайдено PM під час закриття, для PRP-004:** `Microsoft.Extensions.Configuration.EnvironmentVariables` у ContentTool транзитивно **немає** (`obj/project.assets.json`, 2026-11-14), а `ConnectionStrings__Prepround` без нього не прочитати → додано в «Дозволені пакети» PRP-004. User-secrets власника порожні (тека є, `secrets.json` немає) → у PRP-004 ручні команди йдуть через змінну середовища, а власник задає user-secrets на зупинці етапу 1.

### PRP-014 · Читання контенту: абстракція, один крок перевірки, жодного мовчки пропущеного YAML
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** PRP-003 (done), рішення 002
- **Навіщо:**
  - Рішення 002 вимагає, щоб `import` виконував ті самі перевірки, що й `validate`. Зараз послідовність «читач → якщо читач без помилок, правила 1–8» живе лише в `src/Prepround.ContentTool/Program.cs`, а хендлер імпорту в Application не може залежати від `YamlContentReader` з Infrastructure. Без спільного кроку PRP-004 продублював би цю логіку, і `validate` з `import` могли б розійтись: автор бачить «усе гаразд», а імпорт поводиться інакше.
  - Читач мовчки пропускає YAML не на своєму місці: `topics/*.yml`, `topics/*.YAML`, файли тем поза `topics/` (зауваження tester 1 і coder, PRP-003). Наявні теми від цього вже захищає правило 4, бо всі 61 тема є в стеках. Але тема, якої немає в жодному стеку (людина може додати будь-яку тему галузі), просто зникне: `validate` скаже «усе гаразд», а `import` (PRP-004) деактивує її питання. Людина, що готується до співбесіди, втратить тему, і ніхто цього не помітить.
- **Що зробити** (усе в `backend/`):
  1. **`src/Prepround.Application/Content/IContentReader.cs`**: `ContentReadResult Read(string rootDirectory)`. `YamlContentReader` (`src/Prepround.Infrastructure/Content/`) реалізує цей інтерфейс і реєструється в `AddInfrastructure()` (`src/Prepround.Infrastructure/DependencyInjection.cs`).
  2. **Один крок «прочитати й перевірити» в Application** (`Content/`, назва на вибір coder, наприклад `ContentLoader`). Залежить від `IContentReader` і `ContentValidator`, на вході корінь контенту, на виході галузі й повний список `ContentError`. Поведінка та сама, що зараз у `Program.cs`: якщо читач повернув помилки, правила 1–8 не запускаються. Реєструється в `AddApplication()` (`src/Prepround.Application/DependencyInjection.cs`) разом із `ContentValidator`, якщо він потрібен як залежність.
     - `validate` у ContentTool отримує цей крок із DI: `ServiceCollection` + `AddApplication()` + `AddInfrastructure(configuration)`. Це той самий склад, з якого PRP-004 резолвитиме хендлер імпорту. `Program.cs` більше не створює `YamlContentReader` і не викликає `ContentValidator` напряму.
     - `validate` не потребує рядка підключення й не відкриває з'єднання з БД: `AddInfrastructure` реєструє контекст ліниво (PRP-002). Конфігурацію досить зібрати з user-secrets (пакет уже є в ContentTool) або порожньою. Змінні середовища додає PRP-004.
     - Вивід, тексти й коди виходу `validate` не змінюються, крім нових помилок із п. 3.
  3. **Жодного мовчки пропущеного YAML** (принцип правила 9 рішення 002 «помилка, а не тиша», тепер для файлів). Читаються, як і раніше, лише `<галузь>/field.yaml` і файли безпосередньо в `<галузь>/topics/` з розширенням `.yaml`. **Кожен інший файл із розширенням `.yaml` або `.yml` у будь-якому регістрі** в усьому дереві контенту (рекурсивно, включно з коренем) — це `ContentError`: шлях відносно кореня через `/` і підказка українською, де мають лежати файли (`<галузь>/field.yaml`, `<галузь>/topics/<id>.yaml`).
     - Стають помилкою, наприклад: `it/topics/sql.yml`, `it/topics/sql.YAML`, `it/sql.yaml`, `it/topics/extra/sql.yaml`, `sql.yaml` у корені контенту.
     - Як і раніше, пропускаються файли й теки, ім'я яких починається з `.` (`.DS_Store`), а також файли не-YAML (`README.md`, `notes.txt`).
     - Це помилка читача, тож правила 1–8 у цьому прогоні не запускаються, як і для синтаксичних помилок. Так автор не отримує хибних «Теми «sql» немає» на додачу.
- **Дозволені пакети:** нових немає. `Microsoft.Extensions.DependencyInjection` 10.0.15 (`ServiceCollection`, `BuildServiceProvider`) у ContentTool уже доступний транзитивно через EF Core (`obj/project.assets.json`, 2026-11-14). Явний `PackageReference` на нього в ContentTool дозволено (версія через `Directory.Packages.props`, CPM), це та сама DI з рішення 001.
- **Критерії приймання** (команди з `backend/`):
  - [ ] `dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → `it: рівнів 3, напрямів 2, стеків 9, тем 61, питань 36` і `0`
  - [ ] копія контенту, де `it/topics/sql.yaml` перейменовано на `sql.yml` → `validate` дає `1`, у stderr є рядок, що починається з `it/topics/sql.yml:`, і немає рядків «Теми «sql» немає»
  - [ ] те саме для `it/topics/sql.YAML`, `it/sql.yaml` (поза `topics/`), `it/topics/extra/sql.yaml` (підтека) і `sql.yaml` у корені контенту: код `1` і рядок із відповідним шляхом
  - [ ] копія контенту з новою темою `it/topics/extra.yml`, якої немає в жодному стеку → `1` і рядок з `it/topics/extra.yml:` (до цієї картки — тихий `0`)
  - [ ] `.DS_Store` і `README.md` у корені контенту, у `it/` і в `it/topics/` → `0` і той самий підсумок, що на справжньому контенті
  - [ ] `grep -nE "YamlContentReader|ContentValidator" src/Prepround.ContentTool/Program.cs; echo $?` → порожньо і `1`
  - [ ] тест DI: `new ServiceCollection().AddApplication().AddInfrastructure(<конфігурація без рядка підключення>)` з `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })` будується без винятку, а `IContentReader` і крок із п. 2 резолвляться в scope
  - [ ] юніт-тест кроку з п. 2 з ручним фейком `IContentReader`: фейк повернув помилку читача разом із галузями, що порушують правило 4 → у результаті лише помилка читача; фейк без помилок → у результаті помилки правил 1–8
  - [ ] `UsageTests`, `ValidateCommandTests` і `LayerDependencyTests` зелені (Application, як і раніше, без YamlDotNet)
  - [ ] `dotnet build` → `0 Warning(s)`; `dotnet test; echo $?` → `0`, `failed: 0` (точну кількість тестів не фіксуємо)
- **Для tester:**
  - exit-коди — справжнім процесом ContentTool (`tests/Prepround.Tests/ContentTool/ContentToolProcess.cs`), зламані копії — у тимчасовій теці;
  - файлова система власника (APFS) не розрізняє регістр. Якщо `Field.yaml` чи `Topics/` дають помилку, це прийнятно (помилка, а не тиша). Але звичайні `field.yaml` і `topics/*.yaml` хибних помилок давати не мають;
  - тека `topics/`, якої немає, і порожня `topics/` поводяться як до картки;
  - `validate` працює без рядка підключення: user-secrets власника порожні (2026-11-14), автотест не має від цього залежати;
  - решта поведінки з PRP-003 (синтаксичні помилки з рядком, правило 9, `architect` → `0`) не змінилась: наявні тести мають це довести без правок.
- **Контракт API:** без змін.
- **Поза скоупом:** БД, сутності й `import` (PRP-004); нові правила для вмісту, наприклад порожня шкала рівнів («Ідеї»); текст помилки для `correct: [true]` («Ідеї»); будь-які зміни в `content/`.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести: total 178 / failed 0. `ContentLoader` + `IContentReader` в Application, `validate` бере крок із DI, будь-який непрочитаний `.yaml`/`.yml` — помилка читача. Tester оновив `Field_folder_without_field_yaml_is_an_error` (навмисна зміна поведінки: `field.yml` → `field.txt`). Перша картка в режимі «нахил на розробку»: coder ~5 хв, tester ~5 хв.
- **Хвости:**
  - coder, «Помічено поза скоупом»: нічого.
  - tester, «Зауваження» 1: `AddApplication()` без `AddInfrastructure()` з `ValidateOnBuild = true` тепер кидає виняток, бо `ContentLoader` потребує `IContentReader`. Робочі склади (Api, ContentTool) реєструють обидва, тож нічого не ламається. Але це неявна зв'язка: її варто тримати в голові, якщо колись з'явиться склад лише з Application.
  - tester, «Зауваження» 2: рекурсивний обхід у `ReportUnreadYaml` (`YamlContentReader.cs:67-93`) іде за симлінками на теки й не захищений від циклу: цикл дасть переповнення стека, а не помилку. У git-контенті це малоймовірно.
  - tester, «Зауваження» 3: якщо помилково передати `validate` теку галузі (`../content/it`) замість кореня, тепер виходить близько 63 помилок (усі файли «не будуть прочитані») замість однієї. Це помилка, а не тиша, але підсумок шумний. Можна подумати про окрему підказку, якщо в корені лежить `field.yaml`. Це ідея для беклогу, а не дефект.
  - tester, «Зауваження» 4: `IContentReader.Read` синхронний і без `CancellationToken`. Для `validate` цього досить. Для `import` (PRP-004) варто вирішити, чи потрібне скасування. → передано coder PRP-004 приміткою.
- **Розкладено (PM, закриття етапу 1):** tester 1 → прийнято, факт у knowledge (склад «лише Application» не планується); tester 2 і 3 → «Ідеї», «Дрібний борг бекенду»; tester 4 → закрито в PRP-004.

### PRP-004 · Схема БД і `import`
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** PRP-014 (абстракція читача й спільний крок перевірки), PRP-003 (done)
- **Навіщо:** застосунок читає питання лише з бази (рішення 002).
- **Що зробити:**
  1. **Domain, сутності** (усі мають `IsActive`):
     - `Field` (Id, Title);
     - `Level` (FieldId, Id, Title, Rank);
     - `Direction` (FieldId, Id, Title, Order);
     - `Stack` (FieldId, Id, DirectionId, Title, Order) + `StackTopic` (FieldId, StackId, TopicId, Order);
     - `Topic` (FieldId, Id, Title, Description);
     - `Question` (Id, FieldId, TopicId, LevelId, Type, Text, CodeLang?, CodeSource?, Options, Explanation, UpdatedAt `timestamptz` UTC).

     Ключі `(FieldId, Id)` для рівнів, напрямів, стеків і тем, глобальний `Id` для питань. `Options` — jsonb-масив `{text, isCorrect}` у порядку файлу: індекс є `key` (spec 5.2).
  2. **EF-конфігурація і міграція `InitialCatalog`.**
  3. **Application, `ImportContentCommand` + хендлер:**
     - спершу крок «прочитати й перевірити» з PRP-014 (той самий, яким користується `validate`; хендлер отримує його через конструктор). Своєї послідовності «читач → валідатор» хендлер не має. Якщо є помилки, хендлер повертає `Result.Failure(new Error("Content.Invalid", …))`, де `Message` — рядки `файл: id: повідомлення` (як `ContentError.ToString()`), розділені `\n`, і нічого не пише в БД. `Error` — sealed record (`Code`, `Message`), тож список помилок іде саме в `Message`;
     - далі upsert за id з урахуванням рангу й порядку;
     - сутності, яких немає у файлах, отримують `IsActive = false`, а якщо повернулися — `IsActive = true`;
     - `UpdatedAt` питання змінюється лише тоді, коли змінився його вміст;
     - усе в одній транзакції;
     - повертає підсумок: додано / оновлено / деактивовано по кожному типу.
  4. **ContentTool `import <dir>`:**
     - хендлер береться з того самого DI-складу, що й крок для `validate` у PRP-014 (`ServiceCollection` + `AddApplication()` + `AddInfrastructure(configuration)`): ContentTool резолвить `ICommandHandler<ImportContentCommand, …>`, без `new` хендлера чи репозиторіїв;
     - конфігурація: спершу user-secrets (спільний `UserSecretsId` з Api), потім змінні середовища (`AddEnvironmentVariables()`), тож `ConnectionStrings__Prepround` **перекриває** user-secrets. На цьому стоять тести з Testcontainers і ручні команди агентів. Збирання конфігурації — в окремому класі ContentTool, а не в локальній функції top-level `Program`, щоб тест міг перевірити пріоритет, не торкаючись справжніх user-secrets власника (як саме підмінити джерело user-secrets у тесті, вирішує coder і пояснює у звіті);
     - рядка підключення немає або він порожній → код 3 і підказка українською з командою `dotnet user-secrets set …` з `README.md`;
     - успіх → друкує підсумок, код 0;
     - помилки контенту (`Content.Invalid`) → `Message` у stderr, тобто ті самі рядки `файл: id: повідомлення`, що й у `validate` на тому самому контенті, код 1;
     - БД недоступна → код 3 і повідомлення українською;
     - є незастосовані міграції → код 3 і підказка `dotnet ef database update`;
     - **usage:** прибрати позначку «(ще не реалізовано)» біля `import` і гілку «Команду import ще не реалізовано.» з PRP-003. Після цієї картки usage не має жодної такої позначки.
  5. **Рядок підключення для ручних команд.** User-secrets власника для цього `UserSecretsId` порожні (тека є, `secrets.json` немає, 2026-11-14). Агенти їх **не записують**: для `dotnet ef database update` і `import` у критеріях передавай `ConnectionStrings__Prepround="Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=prepround"` (dev-пароль локального контейнера з `README.md`). Власник задасть user-secrets сам на зупинці етапу 1.
- **Дозволені пакети** (версії — у `backend/Directory.Packages.props`, у csproj без `Version`, CPM; обидва — останні стабільні в NuGet на 2026-11-14):
  - Testcontainers.PostgreSql 4.16.0 — лише в Tests, для tester;
  - Microsoft.Extensions.Configuration.EnvironmentVariables 10.0.15 — у ContentTool, для `AddEnvironmentVariables()`. Транзитивно його там немає (перевірено в `obj/project.assets.json`), а без нього `ConnectionStrings__Prepround` не прочитати. Мажорну 11.x не брати (рішення 001: .NET 10).
- **Критерії приймання:**
  - [ ] на чистій БД `dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api; echo $?` → `0`
  - [ ] `import ../content; echo $?` → `0`, підсумок: додано 1 галузь, 3 рівні, 2 напрями, 9 стеків, 61 тему, 36 питань
  - [ ] повторний `import` → `0`; додано 0, оновлено 0, деактивовано 0; `UpdatedAt` не змінився
  - [ ] питання прибрано з файлу (копія контенту) → `import` → рядок є в БД, `IsActive = false`. Питання повернуто → `IsActive = true`
  - [ ] у копії додано рівень `architect` і питання на ньому → `import` → рівень з `Rank = 4`, без змін коду
  - [ ] зламаний контент → код `1`, кількість рядків і `UpdatedAt` у БД не змінились
  - [ ] `import` на зламаній копії друкує в stderr ті самі рядки `файл: id: повідомлення`, що й `validate` на тій самій копії (одна синтаксична помилка, одне порушення правила 6)
  - [ ] після успішного імпорту в копії `it/topics/sql.yaml` перейменовано на `sql.yml` → `import` дає `1`, у stderr рядок з `it/topics/sql.yml:`; жодна тема й жодне питання не деактивовані (кількість рядків, `IsActive` і `UpdatedAt` без змін)
  - [ ] юніт-тест хендлера з ручними фейками: `IContentReader` повертає помилку читача → `Result.Failure` з кодом `Content.Invalid`, у `Message` є рядок цієї помилки, а фейк запису в БД не отримав жодного виклику
  - [ ] Postgres зупинено → код `3`, повідомлення українською
  - [ ] без рядка підключення (null і `""`) → код `3` і підказка з `dotnet user-secrets set`
  - [ ] `ConnectionStrings__Prepround` перекриває user-secrets (тест над тією самою функцією збирання конфігурації, що в ContentTool)
  - [ ] порядок тем стеку й порядок варіантів у БД збігаються з файлами
  - [ ] `dotnet run --project src/Prepround.ContentTool` (без аргументів) → usage без «(ще не реалізовано)», код `2`
  - [ ] тест DI (перший хендлер у проєкті, зауваження 7 tester PRP-002): `new ServiceCollection().AddApplication().AddInfrastructure(config)` з `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })` будується без винятку, і **кожен** зареєстрований `IQueryHandler<,>` / `ICommandHandler<,>` (знайдений рефлексією, а не перелічений вручну) резолвиться в scope. Тест загальний: хендлери PRP-005 і PRP-006 він покриє без змін
  - [ ] `dotnet test; echo $?` → `0`, `failed: 0`
- **Для tester:**
  - інтеграційні тести через Testcontainers;
  - `WebApplicationFactory.UseSetting(key, null)` насправді дає `""` (з PRP-002), тож шлях null перевіряй через `ConfigurationBuilder` / змінну середовища, а не лише через `UseSetting`;
  - автотести не мають залежати від того, чи задав власник user-secrets: зараз їх немає, а на зупинці етапу 1 з'являться. Процес ContentTool проти Testcontainers отримує рядок підключення через змінну середовища. У user-secrets власника не пиши;
  - `validate` після цієї картки й далі працює без рядка підключення (тести PRP-014 зелені);
  - `LayerDependencyTests` лишаються зеленими: в Application немає EF Core і Npgsql, транзакція й upsert — за абстракцією, реалізація в Infrastructure;
  - відкат транзакції при помилці посеред імпорту;
  - зміна лише тексту пояснення → `оновлено 1`.
- **Контракт API:** без змін.
- **Поза скоупом:** ендпоінти, автоматичне застосування міграцій, зміни в читачі й правилах валідації (зроблено в PRP-014), запис у user-secrets власника.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести: total 207 / failed 0 (13 тестів колекції `Postgres` потребують Docker). Сутності каталогу, міграція `InitialCatalog`, `ImportContentHandler` за `ICatalogImportRepository`, `import` з кодами 0/1/2/3, `ContentToolConfiguration` (user-secrets → env). Tester оновив 3 кейси `UsageTests` (навмисна зміна поведінки). БД власника `prepround-postgres:5433` порожня (міграції не застосовано), user-secrets власника порожні.
- **Хвости:**
  - coder, «Помічено поза скоупом»: у `Prepround/README.md` розділ «Content» досі «Coming soon» і не описує `validate`/`import`, коди виходу та обов'язкові `database update` перед `import`.
  - coder, «Помічено поза скоупом»: `AddUserSecrets` на macOS бере корінь з `APPDATA`, якщо змінна задана, інакше з `HOME`. Варто записати в knowledge як шов для процесних тестів.
  - tester, «Зауваження» 1: явна транзакція в `CatalogImportRepository.SaveAsync` поки збігається з поведінкою EF за замовчуванням: один `SaveChanges`, один батч Npgsql. Вона знадобиться, якщо запис колись розіб'ють на кілька `SaveChanges`. Тест відкату таке розбиття ловить (мутація 3b).
  - tester, «Зауваження» 2: `GetStatusAsync` і `LoadAsync` виконуються поза транзакцією запису. Два одночасні `import` можуть зіткнутися на PK, тоді один із них отримає код 3. Даних це не псує, і за карткою це поза скоупом.
  - tester, «Зауваження» 3: підказка без рядка підключення містить `Password=<пароль>` замість `prepround` з README і посилається на README. Критерій «підказка з `dotnet user-secrets set`» виконано, але команду не можна скопіювати й одразу виконати. Варто прийняти рішення (PM).
  - tester, «Зауваження» 4: правило CLAUDE.md «у `backend/src` немає рядка `dotnet`» дослівно порушують 2 рядки підказок у `Prepround/backend/src/Prepround.ContentTool/Program.cs:35,41`. Обидві підказки картка вимагає, тож варто уточнити правило: мова про id каталогу, а не про CLI-команди. → головна сесія уточнила правило в CLAUDE.md.
  - tester, «Зауваження» 5: як і писав coder, розділ «Content» у `Prepround/README.md` досі «Coming soon».
  - tester, «Зауваження» 6: тести тепер потребують Docker для колекції `Postgres` (13 тестів, послідовно). Решта, зокрема `ImportWithoutDatabaseTests`, від Docker не залежить.
- **Розкладено (PM, закриття етапу 1):**
  - coder 1 і tester 5 (README «Content») → PRP-007, п. 11, з критеріями;
  - coder 2 (`AddUserSecrets` і `APPDATA`/`HOME`) і tester 6 (Docker) → knowledge;
  - tester 1 (явна транзакція) → прийнято: тест відкату ловить розбиття на кілька `SaveChanges`;
  - tester 2 (два одночасні `import`) → етап 4, вхідні дані ADR деплою;
  - tester 3 (`Password=<пароль>` у підказці) → **прийнято**: пароль свідомо не в коді (коментар у `Program.cs`), повна команда є в README, а PRP-007 додає туди розділ Content. Літерал пароля в `src` для SecOps-портфоліо гірший, ніж один зайвий крок;
  - tester 4 → головна сесія вже уточнила правило в CLAUDE.md.

### PRP-005 · `GET /api/catalog`
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** PRP-004
- **Навіщо:** фронт будує екран налаштування з каталогу, а рахувати максимум по темі для вибраних рівнів йому допомагають `questionCounts`.
- **Що зробити:**
  - `GetCatalogQuery` + хендлер + read-репозиторій (`AsNoTracking`);
  - `CatalogController`;
  - форма відповіді й порядок — точно за spec 5.1. Лише активні сутності; `questionCounts` рахує лише активні питання; рівні з 0 питань у словник не потрапляють.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] після `import` `curl -sk https://localhost:7120/api/catalog` → 200: 1 галузь; 3 рівні з `rank` 1, 2, 3 у порядку шкали; 2 напрями; 9 стеків по 10 `topicIds` у порядку spec 4.1; 61 тема, відсортована за назвою
  - [ ] `ef-core.questionCounts` → `{"junior":4,"middle":4,"senior":4}`. Тема без питань → `{}`
  - [ ] деактивоване питання не рахується, деактивована тема не потрапляє ні в `topics`, ні в `topicIds`
  - [ ] інтеграційний тест через `WebApplicationFactory` + Testcontainers покриває пункти вище
  - [ ] `curl -sk -i https://localhost:7120/api/catalog -H "Origin: http://localhost:4300"` → є `Access-Control-Allow-Origin: http://localhost:4300` (фронт `PreproundWeb` читає каталог із браузера)
- **Для tester:** JSON у camelCase; рівень, доданий у копію контенту, з'являється в `levels` і `questionCounts` без змін коду. Що `GetCatalogHandler` резолвиться з DI, доводять загальний DI-тест із PRP-004 (має лишитись зеленим без змін) та інтеграційний тест через контролер.
- **Контракт API:** новий, spec 5.1.
- **Поза скоупом:** кешування, фронт.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести: total 213 / failed 0. `GetCatalogQuery` + `GetCatalogHandler` + `CatalogReadRepository` (`AsNoTracking`, лише активні, підрахунок у БД), `CatalogController`. Теми галузі сортуються `StringComparer.InvariantCulture` (латиниця без регістру, потім кирилиця), нічия → `Id` ordinal. БД власника `prepround-postgres:5433` тепер мігрована й наповнена (coder: `database update` + `import` через env-змінну).
- **Хвости:**
  - coder, «Помічено поза скоупом»: нічого.
  - tester, «Зауваження» 1: теми сортуються через `StringComparer.InvariantCulture`. Якщо колись API працюватиме з `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` (так за замовчуванням в офіційних alpine-образах .NET runtime), порівняння стане ordinal і порядок тем зміниться. Наприклад, `AWS` стане перед `Async…`, а `net/http і фреймворки` опиниться після всіх назв із великої латинської літери. Мій тест порівнює з тим самим `InvariantCulture`, тож на такому середовищі він це покаже. Варто згадати в картці деплою.
  - tester, «Зауваження» 2: spec не задає компаратор для «Теми галузі сортуються за назвою». Coder обрав такий порядок: спершу латиниця без урахування регістру, потім кирилиця. Добре б зафіксувати це в spec 5.1 або `knowledge.md`, щоб фронт не пересортовував теми по-своєму (`localeCompare` з `uk` ставить кирилицю першою). → для PRP-009: фронт бере порядок тем з API як є.
  - tester, «Зауваження» 3: без БД `GET /api/catalog` відповідає 500 (виняток EF). Spec 5.1 цього не описує. Поведінка розумна, але її не зафіксовано.
  - tester, «Зауваження» 4: стиль: `CatalogController` не `sealed`.
- **Розкладено (PM, закриття етапу 1):** tester 1 → етап 4 (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`); tester 2 → примітка в PRP-009 (порядок з API) і пропозиція до spec 5.1; tester 3 (`500` без БД) → примітка в PRP-009 (обробка `5xx`), чеклісти етапів 1 і 2, «Ідеї» (`Request.Invalid`); tester 4 → «Ідеї», «Дрібний борг бекенду».

### PRP-006 · `POST /api/tests`: генерація тесту
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** PRP-005
- **Навіщо:** головний сценарій: тест за налаштуваннями людини, з перевагою невиданих питань.
- **Що зробити:**
  1. **`GenerateTestQuery` + хендлер.** Валідація й помилки (spec 5.2), коди:
     - `Test.FieldNotFound`;
     - `Test.LevelsRequired`, `Test.UnknownLevel`, `Test.DuplicateLevel`;
     - `Test.TopicsRequired`, `Test.UnknownTopic`, `Test.DuplicateTopic`;
     - `Test.CountTooSmall`, `Test.TotalOutOfRange`;
     - `Test.NotEnoughQuestions` — повідомлення називає тему й доступну кількість;
     - `Test.TooManyExcluded`.
  2. **`QuestionPicker` в Application — чистий клас без EF.**
     - На вході: кандидати по темах (id + ознака «виключено»), потрібні кількості й `Random`.
     - Спершу бере випадкові невиключені питання, потім добирає з виключених. Дублікатів немає.
     - Перемішує питання всього тесту і варіанти кожного питання. `key` варіанта — його індекс у файлі.
     - `Random` береться з DI: у продакшні `Random.Shared`, тест підставляє `new Random(seed)`.
  3. **Репозиторій** вантажить кандидатів тем (лише id, рівень і ознаку активності), а потім повні дані вибраних питань.
  4. **`TestsController`, `POST /api/tests`:**
     - rate limiter `tests`: fixed window, 30 запитів на хвилину на IP → `429`;
     - ліміт тіла запиту 1 МБ;
     - CORS: браузер перед `POST` з `Content-Type: application/json` шле preflight. Політика за замовчуванням у `src/Prepround.Api/Program.cs` (PRP-002) уже має `WithOrigins(<Cors:Origins>)` + `AllowAnyMethod()` + `AllowAnyHeader()`, а `tests/Prepround.Tests/Api/CorsTests.cs` перевіряє preflight JSON `POST`. **Політику не змінювати.** Вимога: preflight `OPTIONS /api/tests` не отримує `429` і не витрачає ліміт `tests` (інакше «Почати» у веб-MVP отримає мережеву помилку, а фронтові тести з фейковим `ApiService` цього не помітять). Критерій `curl` нижче лишається.
- **Дозволені пакети:** нових немає (rate limiting вбудований).
- **Критерії приймання:**
  - [ ] валідний запит (`ef-core` 5 + `sql` 5, рівні `middle` і `senior`) → 200 і рівно 10 питань: по 5 з кожної теми, лише `middle`/`senior`, без дублікатів, кожне з `explanation` та `isCorrect` у варіантах
  - [ ] сума 9 і 101 → 400 `Test.TotalOutOfRange`. Суми 10 і 100 (за наявності питань) проходять валідацію суми
  - [ ] `count: 0` → 400 `Test.CountTooSmall`. `count` більший за доступний → 400 `Test.NotEnoughQuestions`
  - [ ] невідомі тема чи рівень і дублікати → свої коди 400
  - [ ] 10 001 id у `excludeQuestionIds` → 400 `Test.TooManyExcluded`
  - [ ] тема з 12 питаннями, 8 виключено: запит 4 → жодного виключеного; запит 6 → 4 невиключені + 2 виключені
  - [ ] юніт-тести `QuestionPicker` із фіксованим seed детерміновані (двічі той самий результат)
  - [ ] **варіанти справді перемішані, а `key` — індекс у файлі** (spec 5.2). У всіх 36 seed-питаннях правильні варіанти стоять у файлі першими (зауваження tester, PRP-003), тож без перемішування правильна відповідь завжди була б першою. Тест через `WebApplicationFactory` на імпортованому seed з детермінованим `Random` (seed) з DI, запит на всі 36 питань:
    - є питання, у якого `options[0].isCorrect == false`;
    - є питання, у якого порядок `key` відрізняється від `0, 1, 2, …`;
    - у кожного питання множина `key` дорівнює `{0, …, n−1}`, а `text` і `isCorrect` варіанта з `key = k` збігаються з k-м варіантом цього питання у файлі
  - [ ] 31-й запит за хвилину з однієї IP → 429
  - [ ] 40 preflight `OPTIONS /api/tests` поспіль → жодного `429`, і жоден із 30 наступних `POST` теж не отримує `429` (тест через `WebApplicationFactory`)
  - [ ] `dotnet test; echo $?` → `0`, `failed: 0`
  - [ ] preflight: `curl -sk -i -X OPTIONS https://localhost:7120/api/tests -H "Origin: http://localhost:4300" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type"` → 2xx, є `Access-Control-Allow-Origin: http://localhost:4300`, а `Access-Control-Allow-Headers` містить `content-type`. Той самий запит з `Origin: https://evil.example` → заголовка `Access-Control-Allow-Origin` немає
- **Для tester:**
  - перемішування справді відбувається: на кількох seed порядок різний, а множина та сама;
  - `key` відповідає індексу у файлі;
  - неактивні питання не потрапляють у тест;
  - загальний DI-тест із PRP-004 покриває `GenerateTestHandler` без змін; `Random` у ньому резолвиться з DI;
  - `CorsTests` з PRP-002 лишаються зеленими.
- **Контракт API:** новий, spec 5.2.
- **Поза скоупом:** збереження тестів на сервері, перевірка відповідей на сервері.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести: total 271 / failed 0. `GenerateTestQuery` + `GenerateTestHandler` (11 кодів `Test.*`), статичний `QuestionPicker` з `Random` з DI (`Random.Shared`), `TestGenerationRepository`, `TestsController` з rate limiter `tests` (30/хв на IP) і `RequestSizeLimit` 1 МБ; `UseRateLimiter()` після `UseCors()`, тож preflight ліміт не витрачає. 3 мутації на picker вбито. **Етап 1 завершено.**
- **Хвости:**
  - coder, «Помічено поза скоупом»: ProblemDetails для некоректного JSON розкриває внутрішнє ім'я типу (`Prepround.Application.TestGeneration.TestTopicRequest`) і не відповідає формату помилок spec 5 (`{code, message}`). Варто подумати про `InvalidModelStateResponseFactory` з окремим кодом на кшталт `Request.Invalid` (рішення PM).
  - coder, «Помічено поза скоупом»: у Development відповідь 413 віддає stack trace через developer exception page. У Production цього немає.
  - coder, «Помічено поза скоупом»: rate limiting розділяє клієнтів за `RemoteIpAddress`. За проксі (Tailscale Serve, майбутній деплой) усі клієнти потраплять в один розділ, якщо не налаштувати `ForwardedHeaders`. Варто згадати в картці деплою.
  - coder, «Помічено поза скоупом»: відповідь 429 не має `Retry-After`, і spec цього не вимагає.
  - tester, «Зауваження» 1: `Test.TooManyExcluded` перевіряється останнім, уже після двох запитів до БД (галузь і кандидати). Запит із понад 10 000 id усе одно коштує два звернення до БД. Spec порядку перевірок не фіксує, але перевірку довжини дешевше ставити першою.
  - tester, «Зауваження» 2: синтаксично некоректне тіло (`"count":"x"`, `"levelIds":"middle"`, `null`) дає 400 ProblemDetails від `[ApiController]`, а не `{code, message}`, як вимагає spec 5. Coder це теж помітив. Тут потрібне рішення PM: `InvalidModelStateResponseFactory` з кодом на кшталт `Request.Invalid`. Фронт, який шле типізований JSON, цього не помітить.
  - tester, «Зауваження» 3: ліміт 1 МБ автотестом не покритий: у TestServer немає `IHttpMaxRequestBodySizeFeature`. Я перевірив його лише вручну через curl на Kestrel. Регресію спіймав би тест на `factory.UseKestrel()` (.NET 10), його можна додати пізніше.
  - tester, «Зауваження» 4: rate limiting розділяє клієнтів за `RemoteIpAddress`. За проксі (Tailscale Serve, деплой) усі клієнти потраплять в один розділ без `ForwardedHeaders` (coder теж це зазначив). У відповіді 429 тіло порожнє і немає `Retry-After`, spec цього не вимагає.
  - tester, «Зауваження» 5: `grep -rniE "junior|middle|senior"` по `backend/src` знаходить лише слово «middleware» у коментарі `Program.cs:35`. Це хибне спрацювання, ідентифікаторів каталогу в коді немає. Для майбутніх перевірок варто шукати з межами слова (`\bmiddle\b`).
  - tester, «Зауваження» 6: API під `--launch-profile https` без змінної `ConnectionStrings__Prepround` не має рядка підключення: `POST /api/tests` дає 500 «ConnectionString property has not been initialized». Для агентів це очікувано (CLAUDE.md), але у звіті coder цього кроку не було.
- **Розкладено (PM, закриття етапу 1):**
  - coder 1 і tester 2 (ProblemDetails) → «Ідеї», `Request.Invalid` (P2, до етапу 4); у MVP фронт показує загальний текст (примітка в PRP-009);
  - coder 2 (stack trace на `413` у Development) → прийнято, бо в Production його немає; в етап 4 записано перевірку `ASPNETCORE_ENVIRONMENT=Production`;
  - coder 3 і tester 4 (`ForwardedHeaders`) → етап 4;
  - coder 4 і tester 4 (`Retry-After`), tester 1 (`TooManyExcluded` першим), tester 3 (тест 1 МБ через `UseKestrel`) → «Ідеї», «Дрібний борг бекенду»; факт про TestServer — у knowledge;
  - tester 5 (grep без меж слова) → примітка в PRP-009 і «Мої правила», п. 2;
  - tester 6 (API без рядка підключення → `500`) → чеклісти етапів 1 і 2 і примітка в PRP-009 (`5xx`).

### PRP-007 · Репозиторій `PreproundWeb` і каркас фронту (Angular 22)
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-006 (done; порядок за рішенням власника: спершу весь бекенд), PRP-002 (CORS, health; done), PRP-013 пункт 2б (доступ сесії до теки: у поточній сесії є, див. «Готовність»; npm-кеш і тека вже готові), рішення 003
- **Навіщо:** основа для всіх екранів веб-MVP в окремому репозиторії `PreproundWeb` (рішення 003). Веб-збірка з першого дня йде в окрему теку `dist/web`, щоб майбутня збірка `mobile` отримала свою (урок Outlay, PRP-008).
- **Що зробити:** репозиторій створюється в `/Users/dmitryspain/RiderProjects/PreproundWeb`, поруч із `Prepround`. Далі всі шляхи без префікса — відносно кореня `PreproundWeb`. У `Prepround` не створюємо нічого фронтового (теки `frontend/` там немає).
  1. **Створення проєкту.** Тека `PreproundWeb` уже існує (PRP-013). У ній лише `.idea/` від Rider (PM, 2026-11-14): це дозволено, тож не видаляй і не змінюй її. За кодом CLI (`packages/angular/cli/src/commands/new/cli.ts`) `ng new` не перевіряє, чи тека порожня, а згенерований `.gitignore` містить `.idea/`. Живцем це не перевірено: класифікатор auto mode відхилив пробний запуск PM. Якщо в теці є щось, крім `.idea/`, зупинись і поверни питання. Якщо `ng new` падає через `.idea/` або класифікатор відхиляє саму команду, поверни BLOCKED із точною командою: власник виконає її сам, а ти продовжиш з `git init`.
     - З `/Users/dmitryspain/RiderProjects`: `npx @angular/cli@22.2.7 new prepround-web --directory PreproundWeb --style scss --ssr=false --routing --zoneless --test-runner vitest --ai-config none --package-manager npm --skip-git --interactive=false`. Станом на 2026-11-14 `22.2.7` — це `latest` в npm, а опції звірено з angular.dev/cli/new. Решта налаштувань стандартні (standalone, strict).
     - Потім у корені `PreproundWeb`: `git init`. **Без `git add` і без коміту**: комітить власник. Звичайний `ng new` без `--skip-git` зробив би initial commit, тому його не використовуємо. Ремоут не додаємо.
  2. **`angular.json`:** dev server на порту 4300, `outputPath` → `dist/web`. Скрипти `start` і `build` (production) у `package.json`.
  3. **`src/environments/environment.ts`** — один файл, без `fileReplacements`: `apiBaseUrl: 'https://localhost:7120/api'`. Лише HTTPS-адреса: `http://localhost:5120` під профілем `https` бекенду відповідає `307` на https, а браузер не пропускає CORS-preflight через редирект (перевірено tester у PRP-002).
  4. **`src/app/core/`:**
     - `config.ts` — токен `API_BASE_URL` з environment;
     - `api.service.ts` з `health()`;
     - `models.ts`;
     - `storage.ts` — токен `STORAGE` і реалізація над `localStorage` з JSON:
       - `get<T>(key): T | null` повертає `null`, якщо ключа немає, JSON зіпсований або `localStorage` кидає виняток;
       - `set(key, value): boolean` повертає `false`, якщо запис не вдався (переповнення, недоступність). PRP-012 за цим показує повідомлення;
       - `remove(key)` не кидає.
  5. **Оболонка застосунку:**
     - шапка «Prepround» і `router-outlet`;
     - тема світла й темна за `prefers-color-scheme` на CSS-змінних у `src/styles.scss`;
     - верстка спершу під телефон (веб на телефоні);
     - `src/index.html`: `lang="uk"`, `<title>Prepround</title>`. Мета `viewport` — стандартна від `ng new`.
  6. **Головна сторінка-заглушка** показує «API: доступний» або «API: недоступний» за `health()`.
  7. **`package.json`:** `"engines": {"node": ">=22.22.3 <23"}`. У корені — `.node-version` з `22.23.5`.
  8. **`.gitignore`** лишається той, що створив `ng new` (там є `node_modules` і `dist`).
  9. **`README.md`** (англійською; README від `ng new` замінити повністю):
     - що це: вебклієнт Prepround (тести для підготовки до співбесід), Angular 22;
     - бекенд і контент живуть в окремому репозиторії `Prepround` (локально поруч, `../Prepround`). Як запустити API, описано в його README;
     - вимоги: Node 22.23.5 (`.node-version`), запущений API на `https://localhost:7120` і довірений dev-сертифікат ASP.NET Core (`dotnet dev-certs https --trust`), інакше браузер відкидає запити;
     - команди: `npm install`, `npm start` (`http://localhost:4300`), `npm run build` (`dist/web`), `npx ng test --watch=false`;
     - адреса API лежить у `src/environments/environment.ts`; порт 4300 дозволено в CORS бекенду. API запускати з профілем `https` і ходити лише на `https://localhost:7120`, не на `http://localhost:5120` (там `307`, і браузер відкидає preflight через редирект).
  10. **`CLAUDE.md`** у корені (українською, коротко, до ~60 рядків):
      - що це за репо і що бекенд, контент і вся документація живуть у `../Prepround`;
      - **джерело правди — `../Prepround/backend/.claude/`:** `pm/spec.md` (поведінка, розділ 5 — контракт API), `pm/backlog.md` (картки PRP-NNN), `pm/decisions/` (001 — стек, 003 — окремий репо), агенти `agents/`. Агентів, spec і беклог сюди не копіюємо: цикл агентів запускається із сесії в `Prepround`;
      - **правило контракту між репо** (рішення 003, п. 4): зміна ендпоінта чи DTO в одній картці оновлює і бекенд у `Prepround`, і `src/app/core/api.service.ts` / `models.ts` тут;
      - конвенції фронту з розділу «Фронтенд» у `../Prepround/backend/.claude/CLAUDE.md`: Angular 22 зі стандартними налаштуваннями CLI (standalone, сигнали, zoneless, Vitest, SCSS), без NgRx і UI-бібліотеки; світла й темна тема за `prefers-color-scheme` у CSS-змінних; верстка спершу під телефон; HTTP і маппінг DTO лише в `api.service.ts`, моделі в `models.ts`; адреса API лише через `API_BASE_URL`; локальні дані лише через `STORAGE`, ключі з версією (`prepround.history.v1`); текст контенту не через `[innerHTML]` як є, а через pipe з екрануванням;
      - нічого з каталогу (рівні, галузі, стеки, теми) у `src` не хардкодимо, крім тестів і фікстур;
      - порти: dev server `4300`, API лише `https://localhost:7120` (не `http://localhost:5120`: `307` ламає preflight);
      - команди з п. 9;
      - git: комітить власник, агенти не роблять `commit` / `push`.
  11. **`Prepround/README.md`** (міжрепозиторна правка, англійською):
      - заглушку розділу Frontend з PRP-002 замінити 2–3 рядками: клієнт живе в окремому репозиторії `PreproundWeb`, локально поруч (`../PreproundWeb`), як запустити — у його README;
      - **(PM, закриття етапу 1; хвіст PRP-004)** заглушку розділу Content («Coming soon…») замінити описом того, що вже працює. Факти брати з коду `backend/src/Prepround.ContentTool/Program.cs` і рішення 002, нічого не вигадувати:
        - розкладка: `content/<field>/field.yaml` (шкала рівнів, напрями, стеки з темами) і `content/<field>/topics/<topic-id>.yaml` (одна тема з питаннями). Будь-який інший `.yaml`/`.yml` — помилка. Повний формат і правила — посилання на `backend/.claude/pm/decisions/002-content-format.md`;
        - `dotnet run --project src/Prepround.ContentTool -- validate ../content` без бази: exit `0` (підсумок по галузі), `1` (помилки `file: id: message` у stderr), `2` (аргументи або немає теки);
        - `import ../content`: спершу `dotnet ef database update …` (команда з `backend/.claude/CLAUDE.md`, «Команди»). Імпорт ідемпотентний, питання, яких більше немає у файлах, деактивуються, а не видаляються. Exit `0` / `1` / `2` / `3`, де `3` — база (немає рядка підключення, недоступна, незастосовані міграції). Рядок підключення береться з user-secrets (крок 3 «Run locally»), а змінна `ConnectionStrings__Prepround` його перекриває;
      - у «Run locally» між кроком 3 (user-secrets) і запуском API додати `database update` і `import ../content`: без них `/api/catalog` порожній або дає `500`. У таблиці «Repository layout» у рядку `content/` прибрати «(coming soon)»;
      - решту файла не чіпати.
- **Дозволені пакети:** лише те, що ставить `ng new` для Angular 22.2.7.
- **Критерії приймання** (команди з кореня `PreproundWeb`, якщо не сказано інше):
  - [ ] `test -d .git; echo $?` → `0`; `git rev-list --all --count` → `0`; `git remote` → порожньо
  - [ ] `git check-ignore node_modules dist` → виводить обидва шляхи
  - [ ] `test ! -e /Users/dmitryspain/RiderProjects/Prepround/frontend; echo $?` → `0`
  - [ ] `npm run build; echo $?` → `0`, є `dist/web/browser/index.html`
  - [ ] `npx ng test --watch=false; echo $?` → `0`
  - [ ] `npm start`, потім `curl -s -o /dev/null -w '%{http_code}' http://localhost:4300` → `200`
  - [ ] `grep -rn "localhost:7120" src --include=*.ts` → лише `src/environments/environment.ts`; `grep -rn "localhost:5120" src; echo $?` → порожньо і `1`
  - [ ] `grep -c "prefers-color-scheme: dark" src/styles.scss` → ≥ `1`
  - [ ] `grep -n 'lang="uk"' src/index.html` → знайдено
  - [ ] `node -p "require('./package.json').engines.node"` → `>=22.22.3 <23`; `cat .node-version` → `22.23.5`
  - [ ] `grep -c "\.\./Prepround/\.claude" CLAUDE.md` → ≥ `1`; `test ! -e .claude; echo $?` → `0` (агентів і AI-конфігів від `ng new` немає)
  - [ ] `grep -c "4300" README.md` → ≥ `1`, `grep -c "Prepround" README.md` → ≥ `1`
  - [ ] `grep -c "PreproundWeb" /Users/dmitryspain/RiderProjects/Prepround/README.md` → ≥ `1`
  - [ ] `grep -ci "coming soon" /Users/dmitryspain/RiderProjects/Prepround/README.md` → `0`; `grep -c "database update" …/Prepround/README.md` → ≥ `2` (розділ Content і «Run locally»); `grep -c "ConnectionStrings__Prepround" …/Prepround/README.md` → ≥ `1`; `grep -c "002-content-format" …/Prepround/README.md` → ≥ `1`
  - [ ] `git check-ignore .idea` → `.idea`; `.idea/` після картки на місці й без змін
- **Для tester:**
  - тест головної сторінки з фейковим `ApiService` (доступний / недоступний);
  - `STORAGE`: `localStorage` кидає виняток → `get` дає `null`, `set` дає `false`, нічого не падає; зіпсований JSON → `null`;
  - тести пишуться в `PreproundWeb/src/**/*.spec.ts`, запуск з кореня `PreproundWeb`;
  - `git status` у `PreproundWeb` показує лише untracked-файли, комітів немає. `git add` теж не робити;
  - розділ Content у `Prepround/README.md`: exit-коди й порядок команд звір із `Prepround/backend/src/Prepround.ContentTool/Program.cs` (вибірково, без запуску БД). Пароль у README лише той, що вже був (dev-пароль локального контейнера).
- **Контракт API:** використовує `GET /api/health` (spec 5.3).
- **Поза скоупом:**
  - екрани тесту, i18n;
  - коміти, GitHub-ремоут, CI;
  - копіювання spec, беклогу чи агентів у `PreproundWeb`;
  - будь-які зміни в `Prepround`, крім п. 11;
  - усе для iOS (відкладено, PRP-008): Capacitor, конфігурація й скрипт `mobile`, `environment.mobile.ts`, `dist/mobile`, `core/platform.ts` / `IS_NATIVE_APP`, `viewport-fit=cover`.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести `PreproundWeb`: 18 passed / 0 failed (4 файли). `ng new` (CLI 22.2.7) пройшов без блоку класифікатора, `git init` без комітів. Tester бачив «API: доступний» у headless Chrome з живим API (світла й темна тема) і «API: недоступний» без нього. Головна сесія виправила застарілий коментар у `backend/docker-compose.yml`.
- **Хвости:**
  - coder, «Помічено поза скоупом»: коментар у `Prepround/backend/docker-compose.yml` застарів. → виправлено головною сесією.
  - coder, «Помічено поза скоупом»: рядок 22 у `Prepround/README.md` («The frontend (Angular) lives in a separate repository, `PreproundWeb`.») тепер частково дублює новий розділ Frontend.
  - tester, «Зауваження» 1: `PreproundWeb/.vscode/launch.json` (вивід `ng new`) відкриває `http://localhost:4200/`, а dev server працює на 4300. Для Rider це не важливо, але якщо `.vscode/` лишається, URL варто виправити на 4300.
  - tester, «Зауваження» 2: `WebJsonStorage.set(key, undefined)` повертає `true`, але записує рядок `"undefined"`, і потім `get` дає `null`. Для PRP-012 це варто врахувати: або не передавати `undefined`, або повертати `false`. → примітка coder PRP-012.
  - tester, «Зауваження» 4: рядок 22 `Prepround/README.md` дублює розділ Frontend. Це косметика.
  - tester, «Зауваження» 5: те, що `dotnet ef` бере рядок з `appsettings.Development.json`, живцем не перевірено; стандартна поведінка EF tools.
- **Розкладено (PM, закриття етапу 2):** `docker-compose.yml` — виправлено; рядок 22 README і `.vscode/launch.json` → «Ідеї», дрібний борг фронту; `set(key, undefined)` → примітка PRP-012; `dotnet ef` з `appsettings.Development.json` — прийнято: БД власника вже мігрована, а без рядка підключення команда падає з явною помилкою, тихо нічого не зламається.

### PRP-009 · Екран налаштування тесту
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-005, PRP-006, PRP-007
- **Навіщо:** перший екран, де людина задає свій тест (spec 3.1). У seed питання є лише в 3 з 61 тем, тому екран мусить нормально працювати, коли більшість тем стеку порожні.
- **Що зробити:**
  Усе в репозиторії `PreproundWeb` (рішення 003), шляхи відносно його кореня, команди запускаються з нього.
  1. **`src/app/core/api.service.ts`:** `getCatalog()` і `createTest(request)` з маппінгом DTO → модель (`models.ts`), за spec 5.1–5.2. Помилки:
     - `400` → текст `message` з тіла відповіді;
     - `429` → «Забагато запитів. Спробуйте за хвилину.»;
     - немає відповіді (API не запущено, сертифікат не довірений) → «Не вдалося зв'язатися із сервером.»
  2. **`config.ts`:** `SECONDS_PER_QUESTION = 60` (spec 3.1, п. 6).
  3. **`core/distribution.ts`** — чисті функції:
     - `maxFor(topic, levelIds)` — сума `questionCounts` по вибраних рівнях. Тема з максимумом 0 **неактивна**;
     - `distribute(total, topics)` ділить лише між активними темами: порівну, остача першим, з урахуванням максимумів (те, що не влізло в тему, отримують теми з запасом). Неактивні отримують 0;
     - `validate(setup)`: загальна кількість у межах 10–100; є хоча б одна активна тема; кожна активна має ≥ 1 і ≤ свого максимуму; сума по активних = загальній. Неактивні теми не валідуються й не блокують «Почати».
  4. **Сторінка `/`** (налаштування) за spec 3.1. Вона замінює заглушку з PRP-007, а `health()` лишається в `api.service.ts`:
     - поки каталог вантажиться, показується «Завантаження…». Якщо `getCatalog()` завершився помилкою, замість форми показується текст помилки з п. 1 (для мережевої — «Не вдалося зв'язатися із сервером.»), нічого не падає;
     - галузь (крок прихований, якщо вона одна);
     - напрям і стек, за замовчуванням не вибрані. Вибір стеку підставляє його теми в порядку стеку. Будь-яку тему можна прибрати. «Додати тему» шукає за назвою серед тем галузі, вже додані не пропонує;
     - рівні: за замовчуванням усі, у порядку `rank`, щонайменше один;
     - кількість питань 10–100, за замовчуванням 20;
     - розподіл: поле на кожну активну тему; неактивна тема показана з позначкою «0 питань» і без поля; «Розподілено X з Y»; підказка «Додайте тем або рівнів», якщо Y більше за суму максимумів активних тем;
     - якщо після зміни рівнів максимум теми став 0, тема стає неактивною, а її значення — 0 (spec 3.1, п. 5: значення знижується до максимуму);
     - таймер: перемикач, за замовчуванням увімкнений, підпис «N хв» (N = кількість × `SECONDS_PER_QUESTION` / 60).
  5. **`TestSessionService`** (`core/`, сигнали, у пам'яті):
     - `start(setup)` — **єдине місце**, яке будує тіло `POST /api/tests` (лише активні теми з їхнім `count`, `excludeQuestionIds: []`) і викликає `createTest`. Тест, налаштування і галузь каталогу (назви тем і рівнів для `/test` і `/result`) зберігає в собі. PRP-011 («Пройти ще раз») і PRP-012 («вже бачені») змінюють лише цей метод;
     - «Почати» викликає `start`, поки запит триває, кнопка неактивна, після успіху — перехід на `/test` (сторінка-заглушка до PRP-010). Помилка показується текстом над кнопкою.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] юніт-тести `distribute`:
    - 30 на 4 теми → `[8,8,7,7]`;
    - тема з максимумом 3 при 20 на 2 теми → `[3,17]`, якщо друга тема має ≥ 17 питань;
    - мінімум 1 на активну тему;
    - 20 на 4 теми з максимумами `[12,0,12,12]` → `[7,0,7,6]`
  - [ ] `validate`: 9 і 101 → невалідно; сума ≠ загальній → невалідно; перевищення максимуму → невалідно; активна тема з 0 → невалідно; неактивна тема з 0 → не впливає; жодної активної теми → невалідно
  - [ ] тест компонента з фейковим каталогом, як у seed (стек із 10 тем, питання `{junior:4, middle:4, senior:4}` лише у 3 з них): після вибору стеку — 3 поля зі значеннями 7/7/6, 7 тем з «0 питань», «Почати» активна. Тіло `createTest` точно відповідає spec 5.2: лише 3 активні теми й `excludeQuestionIds: []`
  - [ ] той самий каталог, вибрано один рівень: максимуми стають 4, значення знижуються до 4/4/4, «Розподілено 12 з 20», «Почати» неактивна, підказка показана
  - [ ] «Почати» неактивна, доки X ≠ Y; подвійний клік → один виклик `createTest`
  - [ ] фейковий `ApiService.getCatalog()` повертає мережеву помилку → на `/` текст «Не вдалося зв'язатися із сервером.», форми немає, винятків у консолі тесту немає
  - [ ] `grep -rniE "junior|middle|senior" src --include=*.ts --include=*.html | grep -v "\.spec\.ts"` → порожньо
  - [ ] `npm run build; echo $?` → `0`, `npx ng test --watch=false; echo $?` → `0`
- **Для tester:**
  - помилка API показується текстом `message`, а мережева — повідомленням про сервер, без порожнього екрана;
  - каталог з однією галуззю ховає крок галузі;
  - рівень `architect` у фейковому каталозі показується без змін коду;
  - стек, де всі теми порожні (у seed це Frontend-стеки): нічого не падає, «Почати» неактивна, підказка показана;
  - адреса API лише з `API_BASE_URL` (PRP-007, `https://localhost:7120/api`). `http://localhost:5120` не використовувати ніде: під профілем `https` він відповідає `307`, і preflight `POST /api/tests` через редирект не пройде (зауваження tester PRP-002). Ручна перевірка з браузером — API з `--launch-profile https`.
- **Контракт API:** використовує spec 5.1 і 5.2, без змін.
- **Поза скоупом:** проходження тесту, результат, історія, «вже бачені» (`seen.store`, PRP-012), iOS (PRP-008).
- **Примітки PM (закриття етапу 1, 2026-11-14; доповнюють п. 1, п. 4 і критерії):**
  - **Порядок тем — з API, як є** (PRP-005, tester 2). `topics` галузі сервер уже сортує за назвою: `InvariantCulture`, тобто спершу латиниця без урахування регістру, потім кирилиця. Список «Додати тему» показує теми в порядку відповіді, а пошук лише відфільтровує їх і не пересортовує: `localeCompare` з `uk` поставив би кирилицю першою. Теми стеку йдуть у порядку `topicIds`. Критерій: у фейковому каталозі теми навмисно не за абеткою (`zeta`, `alpha`, `mu`) → у «Додати тему» той самий порядок.
  - **Відповіді, яких немає в п. 1** (PRP-005, tester 3; PRP-006, tester 2 і 6). Якщо API запущено без рядка підключення, `/api/catalog` і `/api/tests` дають `500`, а синтаксично некоректне тіло дає `400` ProblemDetails без `message` (ідея `Request.Invalid`). Обидва випадки → «Сервер не зміг обробити запит. Спробуйте пізніше.». Порожнього або `undefined` тексту бути не може. Критерій — юніт-тест маппінгу помилок `ApiService` через `HttpTestingController`: `400 {code, message}` → `message`; `400` без `message` → загальний текст; `500` → загальний текст; `429` → «Забагато запитів. Спробуйте за хвилину.»; мережева помилка (status `0`) → «Не вдалося зв'язатися із сервером.».
  - **grep-критерій заборон — з межами слова** (PRP-006, tester 5): `grep -rniwE "junior|middle|senior" src --include=*.ts --include=*.html | grep -v "\.spec\.ts"` → порожньо. Без `-w` хибно спрацьовують слова на кшталт `middleware`.
  - Адреса API `https://localhost:7120/api` уже в PRP-007 (п. 3), тут нічого не змінюється.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести `PreproundWeb`: 66 passed / 0 failed. Сторінка `/` (Setup) замінила заглушку `Home` з PRP-007 (її spec видалено). `ApiService.getCatalog/createTest` з маппінгом помилок, `core/distribution.ts`, `TestSessionService.start(setup, field)`, заглушка `/test`. На живому API тіло зі сторінки (7/7/6) → 200, 20 питань. 3 мутації на розподіл вбито.
- **Хвости:**
  - coder, «Помічено поза скоупом»: маршруту `**` немає, тож невідома адреса показує порожній контент (і помилку роутера в консолі).
  - coder, «Помічено поза скоупом»: `README.md` у `PreproundWeb` нічого не каже про екрани, але його картка й не вимагала оновлювати.
  - tester, «Зауваження» 1: після того як рівні звужено, а потім повернуто, значення лишаються зниженими: 4/4/4, «Розподілено 12 з 20». Користувачу доведеться виправляти вручну. Spec вимагає лише знижувати, тож це не баг, але UX можна покращити окремою карткою.
  - tester, «Зауваження» 2: тема, що знову стала активною після зміни рівнів, запускає повний перерозподіл і затирає ручні значення. Це припущення coder, spec цього не описує. Тестом не фіксував.
  - tester, «Зауваження» 3: підказку «Додайте тем або рівнів (доступно питань: 0).» видно одразу після завантаження, коли ще не вибрано жодної теми.
  - tester, «Зауваження» 4: дробове значення загальної кількості (наприклад, 12.5) показує «Від 10 до 100 питань.», хоча число в межах. «Почати» при цьому заблокована правильно.
- **Розкладено (PM, закриття етапу 2):** маршрут `**` — зроблено в PRP-011; README `PreproundWeb` без опису екранів — прийнято (поведінка екранів описана в spec, README — про запуск); tester 1 і 2 → «Ідеї», «Перерозподіл при зміні рівнів» (потрібна правка spec 3.1); tester 3 → PRP-016, п. 3 (підказка лише з вибраними темами); tester 4 → «Ідеї», дрібний борг фронту.

### PRP-010 · Проходження тесту: «Перевірити», блокування, таймер
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-009
- **Навіщо:** головний досвід продукту (spec 3.2–3.3).
- **Що зробити:**
  Усе в репозиторії `PreproundWeb` (рішення 003), шляхи відносно його кореня, команди запускаються з нього.
  1. **`src/app/core/grading.ts`:** `grade(question, selectedKeys)` за spec 3.3 і `score(test, answers)` → `{ correct, total, percent }`, відсоток округлено до цілого.
  2. **`core/clock.ts`:** токен `CLOCK` (`now()` і тік раз на секунду), щоб тести керували часом. Тривалість таймера — `SECONDS_PER_QUESTION` з `config.ts` (PRP-009).
  3. **`shared/content-text.pipe.ts`:** спершу екранує HTML, потім бектики → `<code>`. Блок `code` рендериться як `<pre><code>` текстом. Сирий контент через `[innerHTML]` не потрапляє.
  4. **Сторінка `/test`** за spec 3.2:
     - номер «7 / 30», тема й рівень (назви з каталогу, який зберіг `TestSessionService` у PRP-009), «Назад / Далі», сітка номерів зі станами;
     - radio для `single`, чекбокси й підпис «Кілька правильних відповідей» для `multiple`;
     - кожен варіант — `<label>`, що обгортає radio або чекбокс, на всю ширину, `min-height: 44px` (WCAG 2.5.5), щоб на телефоні в браузері легко влучати пальцем;
     - «Перевірити» → результат, підсвічені правильні варіанти, пояснення, **блокування**;
     - «Завершити тест» з підтвердженням елементом інтерфейсу, якщо є питання без відповіді;
     - таймер (60 с × N, якщо увімкнений), автозавершення на нулі;
     - `beforeunload`, поки тест триває;
     - без активного тесту — редирект на `/`.
  5. **`TestSessionService.finish(answers)`** — єдине місце завершення тесту (кнопка й таймер). Зберігає відповіді, оцінки й витрачений час (з `CLOCK`), потім перехід на `/result` (сторінка-заглушка до PRP-011). PRP-012 допише сюди збереження на пристрої.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] `grade`: `single` правильний / неправильний; `multiple` — точний збіг правильний, підмножина неправильна, надмножина неправильна; без відповіді — неправильно
  - [ ] `score`: 2 з 3 → `percent` 67; 0 з 10 → 0
  - [ ] тест компонента: після «Перевірити» клік і клавіатура не змінюють вибір. Без перевірки вибір змінюється
  - [ ] клік по тексту варіанта (не по самому input) вибирає варіант (тест компонента)
  - [ ] «Перевірити» неактивна без вибраного варіанта
  - [ ] таймер із фейковим `CLOCK`: 3 питання → старт з 180 с; на 0 викликається `finish`. Вимкнений таймер — немає відліку й автозавершення
  - [ ] pipe: `<script>alert(1)</script>` виводиться текстом; `` `AsNoTracking()` `` → `<code>AsNoTracking()</code>`
  - [ ] `grep -rn "innerHTML" src/app --include=*.html` → лише прив'язки через `content-text` pipe
  - [ ] у стилях варіанта є `min-height: 44px` (grep)
  - [ ] прямий перехід на `/test` без тесту → `/`
  - [ ] `npm run build; echo $?` → `0`, `npx ng test --watch=false; echo $?` → `0`
- **Для tester:** стани в сітці номерів; підтвердження — не системний `confirm()`; витрачений час рахується і з вимкненим таймером; варіанти показуються в порядку з відповіді API і **не сортуються за `key`**: у seed правильний варіант у файлі завжди перший (`key = 0`), тож сортування за `key` підказало б відповідь (зауваження tester, PRP-003). Тест компонента з фейковим питанням, де варіанти прийшли в порядку `key` 2, 0, 1, → на екрані той самий порядок.
- **Контракт API:** без змін.
- **Поза скоупом:** екран результату, історія, збереження незавершеного тесту, iOS (PRP-008).
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести `PreproundWeb`: 109 passed / 0 failed. `grading.ts`, `CLOCK`, `contentText` pipe, сторінка `/test` з guard `testInProgress`, `TestSessionService.finish`, заглушка `/result`. Tester пройшов живий сценарій у headless Chrome (API + dev server): .NET → 20 питань → «Перевірити» → блокування (CDP-події клавіатури й миші) → підтвердження → `/result`. 3 мутації вбито.
- **Хвости:**
  - coder, «Помічено поза скоупом»: маршруту `**` досі немає (відомий хвіст PRP-009). → примітка coder PRP-011.
  - coder, «Помічено поза скоупом»: після «Далі» / «Назад» прокрутка не скидається вгору. На телефоні з довгим питанням людині, можливо, доведеться гортати вгору самій. → примітка coder PRP-011.
  - coder, «Помічено поза скоупом»: стилі кнопок `.primary` повторюються в `setup.scss` і `test-run.scss`. Варто винести в спільні стилі окремою карткою.
  - tester, «Зауваження» 1: `/result` без guard і показує «Результату немає.». Це за карткою, guard додасть PRP-011.
  - tester, «Зауваження» 2: прокрутка після «Далі» / «Назад» не скидається вгору. На 390×844 з довгим питанням «Перевірити» і сітка нижче екрана (видно на скріншоті), тож при переході до наступного питання людина опиняється посередині. Варто врахувати в PRP-011 або окремою дрібною карткою.
  - tester, «Зауваження» 3: Chrome робить `pre.code` з горизонтальною прокруткою фокусованим через Tab (keyboard-focusable scrollers). Для a11y це добре, просто на замітку.
  - tester, «Зауваження» 4: підтвердження з'являється на місці кнопки «Завершити тест» внизу сторінки. Зараз воно видиме, бо кнопка остання, але при зміні верстки варто стежити, щоб панель не опинилась поза екраном.
  - tester, «Зауваження» 5: дубль стилів `.primary` / `.secondary` у `setup.scss` і `test-run.scss` (coder сам це відзначив).
- **Розкладено (PM, закриття етапу 2):** `**`, прокрутка і guard `/result` — зроблено в PRP-011; дубль `.primary` / `.secondary` → PRP-016, п. 1; tester 4 (підтвердження внизу сторінки) → PRP-016, п. 4 і критерій «панель у межах вікна 390×844»; tester 3 (`pre.code` фокусується з Tab) — прийнято: для клавіатури це плюс.

### PRP-011 · Сторінка результату
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-010
- **Навіщо:** заради цього людина й проходить тест: побачити, які теми провисають, і прочитати пояснення до своїх помилок (spec 3.4). Остання картка веб-MVP: після неї власник проходить тест по стеку .NET у браузері (чекліст у дорожній карті, етап 2).
- **Що зробити:**
  Усе в репозиторії `PreproundWeb` (рішення 003), шляхи відносно його кореня, команди запускаються з нього.
  1. **`src/app/core/grading.ts`:** `breakdownByTopic(test, answers)` → `[{ topicId, correct, total }]`, від найменшого відсотка до найбільшого. При рівних відсотках — у порядку тем налаштування. Загальний підсумок бере `score` з PRP-010.
  2. **`ResultViewComponent`** показує результат з input-моделі й сам сервіси не читає, щоб PRP-012 перевикористав його для `/history/:id`:
     - відсоток, «X з Y», витрачений час у форматі `M:SS` (від години — `H:MM:SS`);
     - розбивка «тема: правильні / усі» (назви тем із каталогу);
     - розбір: кожне питання з текстом і кодом (через `content-text` pipe), відповіддю людини (або «Без відповіді»), правильними варіантами й поясненням;
     - перемикач «Лише помилки»: лишає неправильні й без відповіді.
  3. **Сторінка `/result`** бере результат із `TestSessionService`. Немає результату (прямий перехід, перезавантаження) → редирект на `/`.
  4. **Кнопки:**
     - «Пройти ще раз з тими самими налаштуваннями» → `TestSessionService.start(setup)` з тими самими налаштуваннями (той самий шлях, що в «Почати», без другої побудови запиту) → `/test`. Помилка API показується на сторінці результату. До PRP-012 `excludeQuestionIds` лишається `[]`, тож питання можуть повторюватись;
     - «Нові налаштування» → `/`.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] `breakdownByTopic` (юніт-тест): теми від найменшого відсотка; рівні відсотки — у порядку налаштування; «правильні / усі» рахуються правильно, питання без відповіді — неправильне
  - [ ] «Лише помилки» лишає тільки неправильні й без відповіді (тест компонента)
  - [ ] витрачений час із фейковим `CLOCK`: 125 с → `2:05`, 3725 с → `1:02:05`
  - [ ] «Пройти ще раз» (фейковий `ApiService`): тіло другого `createTest` дорівнює тілу першого, після успіху перехід на `/test`
  - [ ] «Нові налаштування» → `/`
  - [ ] прямий перехід на `/result` без результату → `/`
  - [ ] `<script>alert(1)</script>` у тексті питання або пояснення в розборі виводиться текстом
  - [ ] `grep -rn "innerHTML" src/app --include=*.html` → лише прив'язки через `content-text` pipe
  - [ ] `npm run build; echo $?` → `0`, `npx ng test --watch=false; echo $?` → `0`
- **Для tester:** автозавершення таймером на нулі → витрачений час дорівнює 60 × N с; результат показується і з вимкненим таймером; блок коду в розборі — `<pre><code>`.
- **Контракт API:** без змін.
- **Поза скоупом:** історія, «вже бачені» й будь-яке збереження на пристрої (PRP-012), підставлення попередніх налаштувань у «Нові налаштування», iOS (PRP-008).
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести `PreproundWeb`: 137 passed / 0 failed. `breakdownByTopic(test, answers, topicOrder)` (третій параметр — порядок тем налаштування), `ResultView` (input-модель, `review: null` для історії), сторінка `/result` з guard `hasResult`, «Пройти ще раз» / «Нові налаштування». Додатково з дозволу головної сесії: прокрутка вгору на `/test` при переходах, `withInMemoryScrolling(top)`, маршрут `**` → `/`. Tester пройшов повний живий сценарій у headless Chrome. **Етап 2 (веб-MVP) завершено.**
- **Хвости:**
  - coder, «Помічено поза скоупом»: форматування часу `M:SS` / `H:MM:SS` тепер є двічі: `TestRun.formatTime` (секунди) і `formatDuration` у `result-view.ts` (мілісекунди). Варто винести в спільну утиліту.
  - coder, «Помічено поза скоупом»: стилі кнопок `.primary` / `.secondary` тепер повторюються вже втретє: `setup.scss`, `test-run.scss`, `result.scss`. Це відомий хвіст PRP-010.
  - coder, «Помічено поза скоупом»: `app.config.ts` ще до мене не проходив `prettier --check`: у `provideHttpClient()` і `]` бракувало кінцевих ком. Так само `setup.spec.ts`.
  - tester, «Зауваження» 1: `grep -rniwE "junior|middle|senior" PreproundWeb/src` (без spec) знаходить коментар у `src/app/test-run/test-run.ts` «…in its middle.». → головна сесія переписала коментар.
  - tester, «Зауваження» 2: сигнатура `breakdownByTopic(test, answers, topicOrder)` має третій параметр, якого в картці немає. Картку PRP-012 (історія зберігає розбивку `breakdownByTopic`) варто доповнити цим параметром.
  - tester, «Зауваження» 3: «рівні відсотки» реалізовано як рівні точні частки: 2/7 стоїть раніше за 5/17, хоча округлено обидва дають 29 %.
  - tester, «Зауваження» 4: `withInMemoryScrolling({ scrollPositionRestoration: 'top' })` юніт-тести не покривають; перевірено лише вживу.
  - tester, «Зауваження» 5: `**` → `''` зберігає query-параметри (`/nope?x=1` → `/?x=1`). Це нешкідливо.
  - tester, «Зауваження» 6: хвости coder (дубль форматування часу, утретє повторені стилі кнопок, `prettier --check`) підтверджую. Це не блокує.
- **Розкладено (PM, закриття етапу 2):** дубль форматування часу і стилів кнопок → PRP-016, п. 1; `prettier --check` → «Ідеї», дрібний борг фронту; коментар «middle» — виправлено; tester 2 (`topicOrder`) → примітка PRP-012; tester 3 — прийнято: сортування за точною часткою чесніше за округлену (2/7 справді слабше за 5/17); tester 4 — прийнято: це конфіг роутера, перевірено вживу; tester 5 — прийнято, нешкідливо.

### PRP-015 · Дизайн-система: теми Aurora, Daylight, Terminal і «Авто», акцент, сторінка `/settings`
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-011 (done), PRP-007 (`STORAGE`)
- **Навіщо:** власник хоче, щоб інтерфейс був гарним і з характером, як в Outlay (2026-11-14). До співбесіди готуються годинами, часто ввечері й з телефона, і тема, яку людина вибрала сама, робить ці години приємнішими. Картка дає основу: токени, три теми, «Авто», акцент і їхній вибір. Наявні екрани одразу перефарбовуються разом із темою, а повний редизайн екранів робить PRP-016.
- **Що зробити:** усе в `PreproundWeb`, шляхи й команди від його кореня. Взірець — Outlay у `/Users/dmitryspain/RiderProjects/OutlayUI-latest/src` (**лише читати**, нічого там не змінювати). Переносимо CSS-токени й підхід, а не Angular 17-код. Prepround — це standalone, сигнали, zoneless, без i18n (рядки українською прямо в шаблонах і `themes.ts`) і без `IS_NATIVE_APP`, `NativeShellService`, `data-native` (iOS відкладено, PRP-008).
  1. **Глобальні стилі.** `src/styles.scss` стає точкою входу і лише підключає через `@use` частини з `src/styles/`:
     - `_tokens.scss` — структурні токени з Outlay `styles/tokens.css`: `--sp-*`, `--fs-*`, `--ease`, `--dur-*`, стеки `--font-ui` і `--font-mono`. **Шрифти лише системні**: Outlay не має ні `@font-face`, ні шрифтових пакетів (Font Awesome там — іконки, їх не беремо). `--nav-w`, density й `--font-serif` / `--font-round` не потрібні;
     - `_themes.scss` — похідні токени `[data-theme] {…}` і три теми з Outlay `styles/themes.css` майже дослівно: `aurora` (темна, скло й градієнтне сяйво), `daylight` (світла), `terminal` (темна, `--font: var(--font-mono)`). Теми задаються лише як набори токенів під `[data-theme="…"]`, а не під `:root`, щоб прев'ю могло мати свою тему. Акцент в OKLCH: `--accent-h: var(--accent-h-user, var(--accent-h-default))`. Атмосфера сторінки `html[data-theme=…] body::before` — для цих трьох тем. Ledger, Brutal, категоріальні палітри `--viz-1…8` і `--av-fg` не переносимо;
     - `_base.scss` — з Outlay `styles/base.css`: reset, типографіка, `:focus-visible`, `.sr-only`, правила View Transitions (`html.theme-vt::view-transition-*`) і `@media (prefers-reduced-motion: reduce)` лише за системним налаштуванням. Перемикача «Рух» у Prepround немає, тож селектори з `data-motion` не потрібні;
     - `_components.scss` — спільні блоки з Outlay `styles/components.css`, які знадобляться екранам у PRP-016: `.card` (+ `.card-head`, `.card-title`, `.card-sub`), `.btn` / `.btn-primary` / `.btn-ghost`, `.input`, `.switch`, `.range`, `.chip`, `.pill` (+ `.pos` / `.neg` / `.warn`), `.callout`, `.eyebrow`, `.page-title`, `.muted`. Тематична структура лише для Aurora (градієнтна `.btn-primary`) і Terminal (`> ` перед `.card-title`, курсор після `.page-title`, сяйво `.btn-primary`). На `pointer: coarse` мінімальна висота кнопок 44px, як в Outlay;
     - `_legacy.scss` — **тимчасовий міст**. Старі змінні PRP-007 визначено через нові токени під `[data-theme]`, щоб наявні екрани одразу перефарбовувались: `--color-text` → `--ink`, `--color-text-muted` → `--ink-3`, `--color-accent` → `--accent`, `--color-on-accent` → `--accent-ink`, `--color-success` → `--pos`, `--color-danger` → `--neg`, `--color-border` → `--line-strong`, `--color-surface` → `--surface-2`, `--color-bg` → `--surface-solid`, `--font-body` → `--font`. Видаляє цей файл PRP-016;
     - старий блок `:root` із кольорами і `@media (prefers-color-scheme: dark)` зі `styles.scss` прибрати. Критерій PRP-007 «`prefers-color-scheme: dark` у `styles.scss`» ця картка свідомо скасовує: системну тему для «Авто» тепер визначають `media.ts` і `theme-init.js`.
  2. **`src/app/core/themes.ts`:**
     - `ThemeId = 'aurora' | 'daylight' | 'terminal'`, `ThemeChoice = ThemeId | 'auto'`, `AUTO_DARK = 'aurora'`, `AUTO_LIGHT = 'daylight'`;
     - `THEMES` (id, назва латиницею, опис українською, `scheme`). Описи: Aurora — «Темна скляна, з живим градієнтним сяйвом»; Daylight — «Світла, чиста й точна»; Terminal — «Моноширинний термінал із фосфорним сяйвом»; «Авто» — «Як у системі: темна — Aurora, світла — Daylight»;
     - `ACCENT_PRESETS` — 8 відтінків Outlay з українськими назвами: Фіалковий 285, Індиго 265, Блакитний 225, Смарагд 160, Лайм 120, Бурштин 75, Кораловий 28, Малиновий 350;
     - `DEFAULT_HUES`: `aurora` 285, `daylight` 265, `terminal` 148. Значення дорівнюють `--accent-h-default` тем у `_themes.scss`, про що є коментар в обох місцях.
  3. **`src/app/core/media.ts`:** DI-токен `MEDIA`, через який застосунок дізнається системні налаштування: сигнал `prefersDark` (`prefers-color-scheme: dark`, оновлюється на `change`) і `prefersReducedMotion()`. За замовчуванням бере їх із `window.matchMedia`. Якщо `matchMedia` немає, то вважає систему світлою, а рух незменшеним, і нічого не кидає. У jsdom `matchMedia` немає (перевірено grep у jsdom 30.1.3), тож без цього шва впаде навіть `app.spec.ts`. Тести підміняють токен фейком, як `CLOCK`.
  4. **`src/app/core/prefs.service.ts`:** `PrefsService` (`providedIn: 'root'`) за взірцем Outlay `core/prefs.service.ts`, але лише з темою й акцентом:
     - сигнали `theme` (`ThemeChoice`, за замовчуванням `'auto'`), `accent` (`number | null`, де `null` — власний акцент теми) і `resolvedTheme` (computed: `'auto'` → `AUTO_DARK` / `AUTO_LIGHT` за `MEDIA.prefersDark`, стежить за зміною системної теми наживо);
     - зберігання **лише через `STORAGE`**, ключ `prepround.prefs.v1`, значення `{ theme, accent }`. Пише тільки після зміни користувачем (`setTheme`, `setAccent`), а старт нічого не пише. Під час читання невідома тема → `'auto'`, а акцент, що не є скінченним числом від 0 до 360, → `null`. Зіпсований JSON (`STORAGE.get` дає `null`) → значення за замовчуванням. Якщо `set` повернув `false`, вибір діє до перезавантаження, без повідомлення: це налаштування, а не дані користувача;
     - на `<html>`: `data-theme` = `resolvedTheme()`; `--accent-h-user` (властивість прибирається, якщо `accent` = `null`); `meta[name="theme-color"]` = обчислене значення токена `--meta`, а якщо воно порожнє (як у jsdom), мета не змінюється;
     - `setTheme(choice, origin?)`: якщо є `document.startViewTransition` і `MEDIA.prefersReducedMotion()` = `false`, нова тема розкривається колом від точки `origin`, як у `transition()` в Outlay (клас `theme-vt`, `clip-path: circle(…)` на `::view-transition-new(root)`, 640 мс). DOM оновлюється синхронно в колбеку. Інакше тема змінюється миттєво. Повторний вибір тієї самої теми нічого не робить. Помилки промісів View Transitions і відсутність `Element.animate` (jsdom) ловляться, як в Outlay;
     - `App` (`src/app/app.ts`) інжектує `PrefsService`, тож тема застосовується на будь-якому маршруті.
  5. **Без спалаху теми до старту Angular:**
     - `public/theme-init.js` — маленький синхронний скрипт, підключений у `<head>` `src/index.html` як `<script src="theme-init.js"></script>`. Він **лише читає** `prepround.prefs.v1` з `localStorage` у `try/catch` і ставить `data-theme` (для `auto` — за `matchMedia`) та `--accent-h-user`;
     - це єдиний виняток із правила «локальні дані лише через `STORAGE`»: скрипт працює до DI і нічого не пише. Він винесений в окремий файл, а не inline, щоб майбутній CSP (етап 4) обійшовся `script-src 'self'` без хешів;
     - у `index.html` також `<meta name="color-scheme" content="dark light">` і `<meta name="theme-color" content="#060913">`. `viewport` не змінювати.
  6. **`src/app/shared/theme-preview/`** — компонент-мініатюра Prepround зі справжніми токенами теми через `[attr.data-theme]`, як в Outlay `theme-preview.component.ts`:
     - «7 / 20», смужка тексту питання, три варіанти (один підсвічений як правильний через `--pos`), кнопка в кольорі акценту й смужка таймера;
     - нічого з каталогу: жодних назв тем, стеків чи рівнів;
     - `auto` — Aurora й Daylight, розділені по діагоналі;
     - `aria-hidden="true"`.
  7. **Сторінка `/settings`** (`src/app/settings/`, маршрут перед `**`):
     - `.eyebrow` «Персоналізація» і заголовок «Налаштування»;
     - картка «Тема оформлення»: `role="radiogroup"` з 4 кнопок `role="radio"` з `aria-checked`: Aurora, Daylight, Terminal, «Авто». У кожної прев'ю, назва й опис. Клік → `setTheme(choice, { x: clientX, y: clientY })`. На ширині 390 px картки тем стоять у дві колонки;
     - «Акцентний колір»: кнопка скидання «Колір теми» (`setAccent(null)`) і 8 кружечків `ACCENT_PRESETS`. У кожного `aria-pressed` і `aria-label` з назвою кольору, область натискання ≥ 44×44 px. Під ними повзунок `input type="range"` 0–360 з `aria-label="Відтінок акценту"`: значення = `accent() ?? DEFAULT_HUES[resolvedTheme()]`, рух повзунка викликає `setAccent`.
  8. **Шапка** (`app.html`):
     - «Prepround» стає посиланням на `/`, поруч посилання «Налаштування» на `/settings`;
     - **поки йде тест** (`TestSessionService.inProgress()`), посилань у шапці немає, лишається текст «Prepround». Відповіді й перевірки живуть лише в компоненті `TestRun` (сигнали `answers` і `checked` у `test-run.ts`), тож перехід із `/test` їх загубить, а повернутися в тест у застосунку нікуди.
  9. **`PreproundWeb/CLAUDE.md`**, розділ «Конвенції»: рядок про світлу й темну тему замінити на опис нової системи:
     - теми Aurora, Daylight, Terminal і «Авто» — це набори токенів під `[data-theme]` у `src/styles/`;
     - кольори в компонентах беруться лише з токенів (`var(--…)`);
     - тему й акцент вибирає `PrefsService` (`prepround.prefs.v1` через `STORAGE`), а `public/theme-init.js` цей ключ лише читає.
- **Дозволені пакети:** нових немає: ні шрифтів, ні іконок, ні UI-бібліотек.
- **Критерії приймання** (з кореня `PreproundWeb`; у grep глоби в лапках, бо shell — zsh):
  - [ ] `PrefsService` (тест із фейковими `STORAGE` і `MEDIA`):
    - без збереженого значення → `theme()` = `'auto'`, `accent()` = `null`;
    - система темна → `html[data-theme="aurora"]`, світла → `"daylight"`;
    - при `'auto'` фейк перемикає системну тему → `data-theme` змінюється без перезавантаження
  - [ ] `setTheme('terminal')` → `data-theme="terminal"` і в `prepround.prefs.v1` записано `{ "theme": "terminal", "accent": null }`; `setAccent(160)` → `document.documentElement.style.getPropertyValue('--accent-h-user')` = `160`, значення збережено; `setAccent(null)` → властивість прибрано
  - [ ] створення сервісу без дій користувача → `STORAGE.set` не викликано
  - [ ] збережене `{ "theme": "brutal", "accent": "x" }` → `'auto'` і `null`; `{ "accent": 400 }` → `null`; зіпсований JSON → значення за замовчуванням, без винятку
  - [ ] View Transitions: з фейковим `document.startViewTransition` виклик `setTheme('daylight', { x: 10, y: 20 })` звертається до фейку один раз і ставить `data-theme="daylight"`; без `startViewTransition` тема змінюється миттєво; з `prefersReducedMotion()` = `true` фейк не викликано
  - [ ] `/settings` (тест компонента):
    - 4 `[role=radio]`, `aria-checked="true"` лише у вибраної теми;
    - клік по Terminal → `theme()` = `'terminal'`;
    - 9 кнопок акценту (скидання + 8), клік по «Смарагд» → `accent()` = `160` і `aria-pressed="true"` лише на ньому;
    - без власного акценту повзунок має значення `DEFAULT_HUES` поточної теми
  - [ ] шапка: є посилання на `/settings` з текстом «Налаштування»; коли `inProgress()` = `true` (фейкова сесія), посилань (`header a`) немає
  - [ ] `grep -rn "prepround.prefs.v1" src public` → лише `src/app/core/prefs.service.ts` і `public/theme-init.js`; `grep -rln "localStorage" src --include='*.ts' | grep -v "\.spec\.ts"` → лише `src/app/core/storage.ts`
  - [ ] `grep -n "theme-init.js" src/index.html` → є, у `<head>`; `grep -c 'name="theme-color"' src/index.html` → `1`
  - [ ] `grep -rnE "@font-face|fonts\.googleapis" src public; echo $?` → порожньо і `1`; у `package.json` ті самі 8 `dependencies` і 7 `devDependencies`, що й до картки
  - [ ] `grep -rn "prefers-color-scheme" src --include='*.scss'; echo $?` → порожньо і `1`
  - [ ] `npm run build; echo $?` → `0` і `npm run build 2>&1 | grep -ci warning` → `0` (бюджети в `angular.json` не змінено); `npx ng test --watch=false; echo $?` → `0`, failed 0, наявні тести зелені
  - [ ] headless Chrome, `http://localhost:4300/settings`, 390×844, для кожної з 3 тем:
    - `data-theme` правильний;
    - `getComputedStyle(document.documentElement).getPropertyValue('--bg')` непорожній;
    - `meta[name=theme-color]` дорівнює `--meta` теми, а `--accent-h-default` — `DEFAULT_HUES` теми;
    - перші дві картки тем мають однаковий `top` (дві колонки);
    - `document.documentElement.scrollWidth` ≤ `390`
  - [ ] headless Chrome: `localStorage['prepround.prefs.v1'] = '{"theme":"terminal","accent":160}'`, перезавантаження → `data-theme="terminal"` і `--accent-h-user` = `160` уже на `DOMContentLoaded`, до старту Angular
- **Для tester:**
  - **Сервери:** API (`https://localhost:7120`) і dev server (`http://localhost:4300`) уже запущені головною сесією для власника. Другий екземпляр не запускати, ці не зупиняти: dev server перезбирається сам. Якщо серверів немає, запусти свої й наприкінці зупини.
  - `app.spec.ts`: `App` тепер інжектує `PrefsService` і `TestSessionService`. Провайдери тесту (фейки `MEDIA`, `ApiService` чи `provideHttpClient`) додати можна.
  - `prefers-reduced-motion: reduce` (CDP `Emulation.setEmulatedMedia`) → тема перемикається без анімації, а сяйво Aurora не рухається.
  - Наявні екрани після картки мають перефарбовуватись разом із темою через `_legacy.scss`: на `/` у Terminal текст не чорний на чорному, кнопка «Почати» в кольорі акценту.
  - Скріншоти 390×844 у звіт (шляхи): `/settings` і `/` у кожній темі, «Авто» при світлій і темній системі.
  - Контраст `--ink` і `--ink-3` на `--bg` і `--surface` — вибірково, на замітку. Якщо значення Outlay перенесено як є, це не FAIL.
- **Контракт API:** без змін.
- **Поза скоупом:**
  - редизайн `/`, `/test`, `/result` і шапки, крім посилань; прибирання дублів `.primary` / `.secondary` і форматування часу — це PRP-016;
  - теми Ledger і Brutal, щільність, перемикач «Рух», приватність, мова, command palette, іконки Outlay;
  - i18n (інтерфейс лише українською);
  - анімації переходів між сторінками («Ідеї»);
  - усе для iOS (PRP-008): `IS_NATIVE_APP`, `data-native`, статус-бар, `viewport-fit=cover`;
  - будь-які зміни в `/Users/dmitryspain/RiderProjects/OutlayUI-latest`.
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести `PreproundWeb`: 185 passed / 0 failed. Токени й теми з Outlay у `src/styles/`, `MEDIA`, `PrefsService`, `theme-init.js`, `theme-preview`, `/settings`, посилання в шапці (ховаються під час тесту). Міст `_legacy.scss` (тут же `--space-page`, `--content-max-width`). Tester перевірив у headless Chrome на 4300: 3 теми, «Авто», справжній View Transition, reduced motion, `theme-init` до старту Angular; контраст `--ink-3` ≥ 4.5 у всіх темах. Головна сесія перезапустила застарілий dev server (watcher не підхоплював зміни).
- **Хвости:**
  - coder, «Помічено поза скоупом»: `PreproundWeb/src/app/app.scss` ще використовує `--color-surface` і `--color-border` у `.header`. Ці рядки існували до картки, їх прибирає PRP-016.
  - coder, «Помічено поза скоупом»: radiogroup тем не має навігації стрілками (roving tabindex за ARIA APG), як і в Outlay: кожна радіокнопка окремо доступна через Tab.
  - coder, «Помічено поза скоупом»: критичний CSS у production (beasties). Якщо побачите спалах при першому завантаженні, можливий варіант — `inlineCritical: false`, але це окреме рішення.
  - tester, «Зауваження» 1: production, критичний CSS (beasties): блоки `[data-theme=…]` з `--bg` не inline-яться, лише в асинхронному `styles-*.css`; перший кадр production може бути без фону теми. На dev server цього немає, на production не вимірювалось. Варіант для майбутньої картки (деплой або PRP-016): `inlineCritical: false` чи інший спосіб гарантувати токени до першого кадру.
  - tester, «Зауваження» 2: активація теми з клавіатури → коло з лівого верхнього кута; у радіогрупі немає навігації стрілками. Як в Outlay.
  - tester, «Зауваження» 3: повзунок відтінку пише в `STORAGE` на кожну подію `input`; можна писати на `change`.
  - tester, «Зауваження» 4: у Terminal сканлайни (`body::before`, `z-index: 90`, `multiply`) візуально притемнюють «Почати». Так задумано в Outlay.
  - tester, «Зауваження» 5: початкова частина `app.spec.ts` не відформатована Prettier ще до картки.

### PRP-016 · Редизайн шапки, `/`, `/test` і `/result` на дизайн-системі
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-015
- **Навіщо:** після PRP-015 екрани лише перефарбовані через тимчасовий міст змінних, а верстка лишилась простою. У `/test` людина проводить десятки хвилин, тож питання, варіанти й пояснення мають легко читатися на телефоні в будь-якій темі. На `/result` слабкі теми мають бути помітні одразу: заради цього людина й проходить тест. Заодно картка прибирає два дублі, про які писали три картки поспіль.
- **Що зробити:** усе в `PreproundWeb`, шляхи й команди від його кореня. Візуальний орієнтир — скріншоти Outlay `/Users/dmitryspain/RiderProjects/OutlayApp/docs/screenshots/overview.png` і `settings.png`: картки на `--surface` з `--radius-l` і `--shadow-card`, заголовки з `--heading-weight`, `.eyebrow` над заголовком сторінки, щедрі відступи `--sp-*`, стани кнопок (hover, active, disabled, focus-visible). Верстка спершу під 390 px.
  1. **Спільне:**
     - кнопки всіх трьох екранів — глобальні `.btn` / `.btn-primary` / `.btn-ghost` з `src/styles/_components.scss` (PRP-015). Локальні `.primary` / `.secondary` у `setup.scss`, `test-run.scss` і `result.scss` видалити (хвости PRP-010 і PRP-011);
     - `src/app/core/format.ts`: `formatDuration(seconds)` → `M:SS`, від години `H:MM:SS`. Ним користуються таймер `/test` (замість `TestRun.formatTime`) і `ResultView` (замість локальної `formatDuration` від мілісекунд; мілісекунди переводяться в секунди на місці виклику з тим самим округленням, що зараз). Хвіст PRP-011;
     - кольори в компонентах беруться лише з токенів PRP-015 (`--ink`, `--ink-3`, `--surface`, `--line`, `--accent`, `--pos`, `--neg`, `--warn`…). `src/styles/_legacy.scss` і його `@use` видалити;
     - спільні для кількох екранів правила виносити в `src/styles/_components.scss`, а не піднімати бюджет `anyComponentStyle` (4 kB) в `angular.json`.
  2. **Шапка:** липка смуга з розмиттям фону; знак-логотип (CSS: заокруглений квадрат у градієнті акценту, без зображень і пакетів) + «Prepround»; праворуч «Налаштування» як `.btn-ghost`. Правило PRP-015 «під час тесту посилань немає» лишається.
  3. **`/` (налаштування тесту):**
     - `.eyebrow` «Підготовка до співбесіди» і `h1.page-title` «Налаштування тесту»;
     - кроки в картках (`.card`): «Напрям і стек», «Рівні», «Питання й теми», «Таймер». Галузь (якщо їх кілька) — у першій картці;
     - рівні — чипи: `label` з класом `.chip` і нативним чекбоксом усередині (візуально прихованим, але доступним з клавіатури), вибраний стан через `:has(input:checked)`;
     - «Розподілено X з Y» з тонкою смужкою прогресу; таймер — `.switch`; «Почати» — велика `.btn-primary` на всю ширину;
     - **підказка «Додайте тем або рівнів»** показується лише тоді, коли вибрано хоча б одну тему. З порожнім списком тем лишається тільки «Виберіть стек або додайте тему.» (хвіст PRP-009, tester 3: зараз попередження видно ще до будь-якої дії).
  4. **`/test`:**
     - верхня смуга: «7 / 20», тонка смужка прогресу (частка питань з відповіддю), таймер як `.pill` (менше 60 с — `--neg`);
     - питання в картці, тема й рівень над ним як `.eyebrow`;
     - варіанти — картки на всю ширину зі станами: наведення, вибраний (акцент), правильний (`--pos`), хибний (`--neg`); `min-height: 44px` лишається;
     - після «Перевірити» — `.callout` зеленого або червоного відтінку з вердиктом і поясненням;
     - «Назад / Далі» — `.btn`, сітка номерів зі станами через токени, «Завершити тест» — `.btn-ghost`;
     - **панель підтвердження** після «Завершити тест» має бути видно на екрані 390×844 без ручної прокрутки (хвіст PRP-010, tester 4), наприклад через `scrollIntoView` панелі.
  5. **`/result`:**
     - великий відсоток (`--fs-hero`), «X з Y» і витрачений час поруч, як `.pill`;
     - «Теми» — картка зі смужками. Колір смужки залежить від частки правильних: `< 50 %` → `--neg`, від 50 до `< 80 %` → `--warn`, `≥ 80 %` → `--pos`. На `.topic` ставиться атрибут `data-band="low" | "mid" | "high"`, межі — константи поруч із `ResultView`. Так слабкі теми видно одразу;
     - розбір — картка на кожне питання, вердикт як `.pill`;
     - дії: «Пройти ще раз з тими самими налаштуваннями» (`.btn-primary`) і «Нові налаштування» (`.btn`).
- **Тести, на які спираються наявні екрани.** Ці класи й структуру лишаємо, нові класи можна додавати поруч:
  - `header` з текстом «Prepround»;
  - `/test`: `.position`, `.timer[role=timer]`, `.meta`, `.text`, `.text code`, `pre.code > code`, `.options input`, `.option` з `.selected` / `.correct` / `.wrong`, `.option-text`, `.verdict`, `.explanation`, `.cell[data-state]`, `[aria-current]`, `.confirm[role=alertdialog]`;
  - `/`: `.hint`, `.error[role=alert]`, `.topic`, `.topic.inactive`, `.topic-title`, `input.number`, `button.candidate`, `select`, `input[role=switch]`. Тести читають текст батьківського елемента чекбокса рівня (назва рівня) і перемикача таймера («Таймер: N хв» / «вимкнено»);
  - `/result`: `label.check` («Лише помилки»), `.topic-title`, `.topic-score`, `.review`, `.review .meta`, `.review .verdict`, `.percent`, `.count`, `.elapsed`;
  - кнопки тести шукають за текстом: «Перевірити», «Далі», «Назад», «Завершити тест», «Завершити», «Продовжити тест», «Пройти ще раз з тими самими налаштуваннями», «Додати тему». Ці тексти не змінювати;
  - єдиний селектор кнопки за класом — `button.primary` у `setup.spec.ts`. Якщо «Почати» отримує `.btn-primary`, tester оновлює селектор. Будь-яку іншу навмисну зміну верстки, яка ламає тест, tester виправляє в цій картці, перевіряючи той самий факт, і перелічує правки у звіті.
- **Дозволені пакети:** нових немає.
- **Критерії приймання** (з кореня `PreproundWeb`; глоби в лапках, shell — zsh):
  - [ ] `formatDuration` (юніт): `0` → `0:00`, `125` → `2:05`, `3599` → `59:59`, `3600` → `1:00:00`, `3725` → `1:02:05`; `grep -rnE "formatTime|function formatDuration" src/app --include='*.ts' | grep -v "\.spec\.ts"` → лише `src/app/core/format.ts`
  - [ ] `grep -rnE "^\s*\.(primary|secondary)\b" src/app --include='*.scss'; echo $?` → порожньо і `1`
  - [ ] `grep -rn -- "--color-" src; echo $?` → порожньо і `1`; `test ! -e src/styles/_legacy.scss; echo $?` → `0`
  - [ ] `grep -rnE "#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(" src/app --include='*.scss'; echo $?` → порожньо і `1` (кольори компонентів лише з токенів)
  - [ ] `/` (тест компонента): без вибраного стеку підказки «Додайте тем або рівнів» немає, а «Виберіть стек або додайте тему.» є; сценарій PRP-009 з одним рівнем (4/4/4, «Розподілено 12 з 20») — підказка є
  - [ ] `/result` (тест компонента): теми з 1/3, 2/3 і 5/5 правильних мають `data-band` `low`, `mid` і `high`; межі: 1/2 → `mid`, 4/5 → `high`, 79/100 → `mid`, 0/1 → `low`
  - [ ] `npm run build; echo $?` → `0` і `npm run build 2>&1 | grep -ci warning` → `0` (бюджети не змінено); `npx ng test --watch=false; echo $?` → `0`, failed 0
  - [ ] `grep -rniwE "junior|middle|senior" src --include='*.ts' --include='*.html' | grep -v "\.spec\.ts"` → порожньо; `grep -rn "innerHTML" src/app --include='*.html'` → лише прив'язки через `contentText`
  - [ ] headless Chrome, 390×844, у кожній із 3 тем: .NET → 20 питань → «Перевірити» на правильній і хибній відповіді → «Завершити тест» з питаннями без відповіді. Одразу після натискання панель `.confirm` має `getBoundingClientRect().bottom` ≤ `844` і `top` ≥ `0`. Далі «Завершити» → `/result`
  - [ ] headless Chrome, 390×844: на `/`, `/test`, `/result` і `/settings` `document.documentElement.scrollWidth` ≤ `390` (немає горизонтальної прокрутки сторінки); довгий рядок у `pre.code` прокручується всередині блока
- **Для tester:**
  - **Сервери:** як у PRP-015. API й dev server уже запущені, другий екземпляр не запускати, ці не зупиняти.
  - Скріншоти у звіт (шляхи): 390×844 і 1280×800, `/`, `/test` до й після «Перевірити», `/result` — у кожній із 3 тем.
  - Фокус з клавіатури (`:focus-visible`) видно на кнопках, чипах рівнів і варіантах у кожній темі.
  - Неактивна «Перевірити» і неактивна «Почати» виглядають неактивними і в Terminal.
  - Рівні-чипи працюють з клавіатури (Tab + Space), а останній вибраний рівень не знімається, як і раніше.
- **Контракт API:** без змін.
- **Поза скоупом:**
  - історія й «вже бачені» (PRP-012);
  - нова поведінка налаштування: перерозподіл при зміні рівнів («Ідеї»);
  - анімації переходів між сторінками («Ідеї»), іконки-пакети;
  - дрібний борг фронту з «Ідей» (`launch.json`, `prettier --check`, текст для дробової кількості);
  - iOS (PRP-008).
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола (coder перервався на ліміті API й продовжив); тести `PreproundWeb`: 193 passed / 0 failed. `_legacy.scss` видалено, кнопки `.btn*`, `core/format.ts`, чипи рівнів, смужки прогресу, `.callout` вердикту, `data-band` тем (`topicBand`, межі 50/80 за точною часткою), липка смуга тесту, `.confirm` у межах екрана. Tester у headless Chrome: 3 теми × 390/1280, `scrollWidth` у нормі, кільця фокуса, неактивні кнопки, на `/result` усі три смуги. **Етап 2a «Оформлення» завершено.**
- **Хвости:**
  - coder, «Помічено поза скоупом»: панель `.confirm` (`role="alertdialog"`) не забирає фокус; фокус падає на `body`. Так було й до картки.
  - coder, «Помічено поза скоупом»: хвіст PRP-015 про критичний CSS (beasties) у production не перевіряв і не чіпав.
  - tester, «Зауваження» 1: сітка номерів у Terminal: `--accent` і `--pos` обидва зелені, тому стани `answered` і `correct` розрізняє лише відтінок фону, а `unanswered` відрізняється слабо. Візуально варто подумати про позначку ✓ або інший контраст.
  - tester, «Зауваження» 2: проміжок 8 px між шапкою і липкою смугою тесту: коли питання прокручене, у щілині видно край картки. Coder зробив це свідомо, але на телефоні схоже на артефакт. Хай власник вирішить.
  - tester, «Зауваження» 3: inline-код у тексті питання переноситься посеред виразу (`db.Users.FirstOrDefault(u => u.Id` / `== id)`). Ідея.
  - tester, «Зауваження» 4: фокус після «Завершити тест» (як у coder).
  - tester, «Зауваження» 5: у Chrome 130+ прокручувані `pre.code` фокусовані з клавіатури. Коректно, на замітку.

### PRP-012 · Історія і «вже бачені»
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** PRP-011
- **Навіщо:** людина бачить свої минулі результати, а наступний тест віддає перевагу питанням, яких вона ще не бачила (spec 3.5). На 36 питаннях seed повтори помітні вже з другого тесту.
- **Що зробити:**
  Усе в репозиторії `PreproundWeb` (рішення 003), шляхи відносно його кореня, команди запускаються з нього.
  1. **`src/app/core/seen.store.ts`** над `STORAGE`, ключ `prepround.seen.v1`, формат `{ [topicId]: string[] }`:
     - `forTopics(topicIds): string[]` — id «вже бачених» лише цих тем;
     - `add(questions)` — без дублікатів;
     - `clear()`.
  2. **`core/history.store.ts`** над `STORAGE`, ключ `prepround.history.v1`. Запис за spec 3.5:
     - `id` — мітка часу завершення в мс із `CLOCK` (тести детерміновані);
     - `finishedAt` — ISO-рядок UTC (із `Z`);
     - налаштування (галузь, рівні, теми з кількостями, таймер);
     - `correct`, `total`, `percent`, витрачений час, розбивка по темах (`breakdownByTopic`);
     - відповіді: `{ questionId, selectedKeys }`;
     - не більше 200 записів, найстаріші видаляються.
  3. **`TestSessionService`:**
     - `finish` записує результат в історію і додає в «вже бачені» **всі питання тесту** (це й є «показані питання» зі spec 3.5);
     - `start` надсилає `excludeQuestionIds` = `seen.forTopics(...)` для тем запиту замість `[]`. «Пройти ще раз» отримує це автоматично, бо йде через `start`.
  4. **Сторінки:**
     - `/history`: список, новіші вгорі (дата локально, теми, відсоток), натискання відкриває `/history/:id`. Порожня історія → «Історія порожня»;
     - `/history/:id`: `ResultViewComponent` з PRP-011 без розбору питань (тексти питань в історії не зберігаються). Невідомий id → `/history`;
     - «Очистити історію» з підтвердженням елементом інтерфейсу (не `confirm()`): стирає обидва ключі;
     - у шапці посилання «Історія».
  5. **Збій сховища:** `STORAGE.set` повернув `false` → на `/result` показується «Не вдалося зберегти результат на пристрої.», а результат поточного тесту видно як завжди.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] після `finish` (тест сервісу з фейковими `STORAGE` і `CLOCK`) у `prepround.history.v1` один запис з усіма полями п. 2, `finishedAt` закінчується на `Z`; у `prepround.seen.v1` — id усіх питань тесту за темами
  - [ ] повторний `add` тих самих id не дублює їх
  - [ ] `start`: `excludeQuestionIds` містить «вже бачені» лише вибраних тем, id іншої теми з фейкового сховища не надсилаються
  - [ ] «Пройти ще раз» після тесту → `excludeQuestionIds` містить усі питання щойно пройденого тесту (фейковий `ApiService`)
  - [ ] 201-й запис витісняє найстаріший, лишається 200
  - [ ] `/history`: новіші вгорі; порожня → «Історія порожня»; `/history/<невідомий id>` → `/history`
  - [ ] «Очистити історію»: після підтвердження обидва ключі видалено; «Скасувати» → нічого не змінилось
  - [ ] `STORAGE.set` повертає `false` → `/result` рендериться й показує повідомлення про збереження
  - [ ] зіпсований JSON у `prepround.history.v1` → історія порожня, застосунок не падає
  - [ ] `npm run build; echo $?` → `0`, `npx ng test --watch=false; echo $?` → `0`
- **Для tester:** ключі мають суфікс `.v1`; у сховищі дати в UTC, а на екрані локальні; `/history/:id` без розбору питань — так задумано (див. «Ідеї»).
- **Контракт API:** без змін (використовує `excludeQuestionIds` зі spec 5.2).
- **Поза скоупом:** розбір питань в історії, «Пройти ще раз» з історії, синхронізація між пристроями, експорт, `@capacitor/preferences` (PRP-008).
- **Примітки PM (закриття етапу 2, 2026-11-14; доповнюють п. 2–4 і критерії):**
  - **Розбивка в записі історії** — `breakdownByTopic(test, answers, topicOrder)` з PRP-011: третій параметр — порядок тем налаштування (PRP-011, tester 2).
  - **`STORAGE.set(key, undefined)`** повертає `true`, але записує `"undefined"`, і `get` потім дає `null` (PRP-007, tester 2). У `history.store` і `seen.store` не передавати `undefined` (лише масиви й об'єкти).
  - **Оформлення — на дизайн-системі PRP-015/016:** `/history` і `/history/:id` будуються з `.card`, `.btn*` і токенів (`--ink`, `--surface`, `--pos`, `--neg`…), без `--color-*` і сирих кольорів; час — `formatDuration` з `src/app/core/format.ts`; `ResultView` з PRP-016 уже має смужки тем із `data-band`.
  - **Посилання «Історія» в шапці** — поруч із «Налаштування» і за тим самим правилом PRP-015: поки йде тест (`inProgress()`), посилань у шапці немає.
  - **«Очистити історію» не чіпає `prepround.prefs.v1`** (тема й акцент, PRP-015). Критерій: після підтвердження `prepround.history.v1` і `prepround.seen.v1` видалено, а `prepround.prefs.v1` на місці.
- **Примітки PM (ADR 006, 2026-11-19; якщо PRP-012 іде після PRP-027):**
  - **Тексти** `/history`, `/history/:id` і «Очистити історію» живуть лише в словниках `core/i18n/messages/uk.ts` і `messages/en.ts`. Критерій grep із PRP-027 («кирилиці поза `i18n/` і spec немає») лишається зеленим.
  - **Запис історії від мови не залежить:** лише id, налаштування й числа, без назв тем. Назви `/history` бере з каталогу поточною мовою, а невідомий id показує як є. Поля мови в записі немає.
  - **Дата** форматується через `Intl.DateTimeFormat(<поточна мова>)`, без `DatePipe` і `LOCALE_ID`. Критерій: та сама мітка часу показується по-різному для `uk` і `en`.
  - **«Вже бачені»** спільні для мов, бо id питань ті самі. Перемикання мови їх не скидає.

### PRP-013 · Підготувати середовище до `PreproundWeb` і оновити .NET SDK
- **Статус:** todo
- **Виконавець:** власник
- **Пріоритет:** P1 (пункти 1–2), P2 (пункт 3)
- **Область:** frontend + backend
- **Платформа:** web
- **Залежить від:** рішення 003
- **Навіщо:**
  - PRP-007 створює репозиторій у `/Users/dmitryspain/RiderProjects/PreproundWeb`, тобто поза текою, з якої запущено сесію Claude Code (`Prepround`). Без доступу до цієї теки coder і tester можуть упертися в запити дозволів посеред циклу, а субагент на них відповісти не може.
  - У кеші npm є файли root (залишок PRP-001, пункт 4), і через них npm уже падає. Перевірено 2026-11-14: `npx @angular/cli@22.2.7 new … --dry-run` → exit 243, `EACCES: permission denied, rename … ~/.npm/_cacache/content-v2/…`. З окремим чистим кешем та сама команда дає `0`, тобто опції `ng new` з PRP-007 правильні, а заважає лише кеш. Без цього пункту PRP-007 отримає BLOCKED.
  - SDK 10.0.100 / runtime 10.0.0 відстали на 15 патчів безпеки. Не блокує: `global.json` бере новіший SDK сам.
- **Що зробити:**
  1. ~~**Обов'язково до PRP-007. npm-кеш:** `sudo chown -R 501:20 ~/.npm`.~~ Виконано інакше, без sudo (див. «Виконано»).
  2. **Обов'язково до PRP-007. Доступ сесії до `PreproundWeb`:**
     - а) ~~створити порожню теку: `mkdir /Users/dmitryspain/RiderProjects/PreproundWeb` (`ng new --directory` пише в порожню теку)~~ — виконано;
     - б) дати сесії доступ до неї одним зі способів:
       - постійно: у `Prepround/backend/.claude/settings.local.json` додати `"permissions": { "additionalDirectories": ["/Users/dmitryspain/RiderProjects/PreproundWeb"] }` (локальний файл, у git не додавати);
       - на одну сесію: команда `/add-dir /Users/dmitryspain/RiderProjects/PreproundWeb`.
  3. **За бажанням. .NET SDK 10.0.404 і runtime 10.0.15** (актуальні на 2026-11-14, вийшли 2026-11-10): https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-osx-arm64.pkg. Тести PRP-002 йшли на runtime 10.0.0 (зауваження tester).
  4. **За бажанням, прибирання.** Старий кеш перейменовано на `~/.npm-root-owned-backup`, у ньому лишились файли root. Коли PRP-007 пройде, його можна видалити: `sudo rm -rf ~/.npm-root-owned-backup`.
- **Критерії приймання:**
  - [x] `find ~/.npm -user root | wc -l` → `0` (PM, 2026-11-14)
  - [x] `test -d /Users/dmitryspain/RiderProjects/PreproundWeb && test -z "$(ls -A /Users/dmitryspain/RiderProjects/PreproundWeb)"; echo $?` → `0` (PM, 2026-11-14)
  - [ ] `grep -c PreproundWeb /Users/dmitryspain/RiderProjects/Prepround/backend/.claude/settings.local.json` → ≥ `1`, або власник підтвердив `/add-dir` у поточній сесії
  - [ ] (пункт 3) `dotnet --list-sdks` містить `10.0.404`, `dotnet --list-runtimes` містить `Microsoft.AspNetCore.App 10.0.15`
- **Виконано (2026-11-14):**
  - пункт 1: старий `~/.npm` перейменовано на `~/.npm-root-owned-backup`, новий `~/.npm` без файлів root. Перевірка справжньою командою PRP-007 (правило 21): `npx @angular/cli@22.2.7 new prepround-web --directory PreproundWeb … --dry-run` у тимчасовій теці → exit `0` (раніше 243, `EACCES`);
  - пункт 2а: тека `PreproundWeb` існує. На ~03:00 у ній з'явилась `.idea/` від Rider: вона дозволена (примітка в PRP-007, п. 1);
  - пункт 2б **для поточної сесії**: PM записав і видалив пробний файл у `PreproundWeb` через Bash (`touch`/`rm` → `0`) і через Write (2026-11-14 ~03:00), запитів дозволу не було. Тож PRP-007 можна брати в цій сесії.
- **Лишилось:**
  - пункт 2б **постійно**: файла `Prepround/backend/.claude/settings.local.json` немає (перевірено 2026-11-14 ~03:00). У новій сесії доступ не гарантований: або `additionalDirectories` (один раз), або головна сесія перед PRP-007 перевіряє запис пробним файлом і за потреби просить `/add-dir`;
  - на замітку: класифікатор auto mode відхилив пробний `npx -y @angular/cli@22.2.7 new … --skip-install` у scratchpad PM (2026-11-14). Якщо він відхилить і `ng new` у PRP-007, coder поверне BLOCKED із командою, а власник виконає її сам із `/Users/dmitryspain/RiderProjects` (~1 хв) або підтвердить дозвіл;
  - пункт 3 (за бажанням): досі SDK 10.0.100 і runtime 10.0.0;
  - пункт 4 (за бажанням).

### PRP-017 · Необов'язкове поле `sources` у питанні: формат і правило 10
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** рішення 004 (accepted 2026-11-14; `sources` **необов'язкове** за рішенням власника), PRP-003, PRP-014 (done)
- **Навіщо:** `content-author` (PRP-018 і далі) додає до питань посилання на сторінку документації, яка підтверджує правильну відповідь, а tester звіряє за ними. Зараз невідомий ключ `sources` — помилка читача (правило 9), тож без цієї картки питання з джерелами не пройдуть `validate`. Обов'язковим поле не робимо (рішення власника 2026-11-14, «згодом ще подумаємо»), тому 36 seed-питань не чіпаємо.
- **Що зробити** (у `backend/`, якщо не сказано інше):
  1. `src/Prepround.Application/Content/QuestionDefinition.cs`: новий останній параметр `IReadOnlyList<string> Sources`.
  2. `src/Prepround.Infrastructure/Content/YamlContentReader.cs`:
     - `sources` додати в `QuestionKeys`;
     - значення — послідовність рядків. Ключа немає → порожній список;
     - значення не послідовність або елемент не скаляр → помилка читача з файлом і рядком, як для інших ключів неправильної форми.
  3. `src/Prepround.Application/Content/ContentValidator.cs`, **правило 10** (рішення 004, розділ 3), лише коли ключ `sources` є в питанні:
     - список непорожній (`sources: []` — помилка: або джерело, або без ключа);
     - кожен елемент — абсолютний URI зі схемою `https` (`Uri.TryCreate(…, UriKind.Absolute, …)` і `Scheme == "https"`) без пробільних символів;
     - без повторів у межах питання (ordinal);
     - повідомлення українською з id питання, у тому ж форматі, що й інші правила. У XML-doc класу «Rules 1–8» замінити на «Rules 1–8 and 10».

     Питання без `sources` лишається валідним. Щоб розрізнити «ключа немає» і `sources: []`, читач може передавати `null` замість порожнього списку — на вибір coder, пояснити у звіті.
  4. `import`, сутність `Question`, міграції й API **не змінюються**: `sources` живе лише в YAML (рішення 004, розділ 3).
  5. `README.md` (корінь репо), розділ «Content»: одне речення англійською про необов'язковий `sources` з посиланнями на офіційну документацію (рішення 004).
- **Дозволені пакети:** нових немає.
- **Критерії приймання** (команди з `backend/`, якщо не сказано інше; shell — zsh, глоби в лапках):
  - [ ] `dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → `it: рівнів 3, напрямів 2, стеків 9, тем 61, питань 36` і `0` (seed без `sources` валідний)
  - [ ] у копії контенту в тимчасовій теці до `sql-001` додано `sources: [https://www.postgresql.org/docs/18/ddl-constraints.html]` → `0`
  - [ ] у копії кожна з поломок `sources` у `sql-001` дає `validate` → `1` і рядок із `sql.yaml` та `sql-001`: `sources: []`; `http://…`; відносний `www.postgresql.org/docs/18/`; два однакові URL; URL із пробілом
  - [ ] `sources: https://www.postgresql.org/docs/18/` (рядок замість списку) → `1` і рядок із файлом і номером рядка (помилка читача)
  - [ ] `ConnectionStrings__Prepround="Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=prepround" dotnet run --project src/Prepround.ContentTool -- import ../content; echo $?` → `0` і рядок `Питання: додано 0, оновлено 0, деактивовано 0`
  - [ ] `ContentValidatorTests`, `YamlContentReaderTests`, `ImportCommandTests`, `ValidateCommandTests`, `LayerDependencyTests`, `HandlerCompositionTests` зелені без правок фікстур; `dotnet build` → `0 Warning(s)`; `dotnet test; echo $?` → `0`, `failed: 0`
- **Для tester:**
  - наявні YAML-фікстури без `sources` мають лишитися валідними (поле необов'язкове);
  - копія з рівнем `architect` і питанням із `sources` → `0`;
  - мутація правила 10 (`https` → будь-яка схема) має ловитися тестом;
  - питання з `sources` проходить `import` і дає той самий рядок у БД, що й без нього (поле в БД не потрапляє).
- **Контракт API:** без змін.
- **Поза скоупом:** обов'язковість `sources` (рішення власника — пізніше); джерела для 36 seed; БД, міграція, `import`, API, показ джерела в UI («Ідеї», «Детальніше»); нові питання (PRP-018 і далі); білий список доменів у коді (політика ADR 004).
- **Результат (2026-11-14, головна сесія):** PASS з 1 кола; тести бекенду: total 307 / failed 0. `QuestionDefinition.Sources` (`IReadOnlyList<string>?`: `null` — ключа немає, `[]` — помилка), читач, правило 10 у `ContentValidator`, речення в README. 3 мутації вбито.
- **Хвости:**
  - coder, «Помічено поза скоупом»: `ContentLoader.cs:23` — коментар «Rules 1–8 assume every file was read…» тепер стосується і правила 10.
  - coder, «Помічено поза скоупом»: `ContentValidatorTests.cs:5` summary «Rules 1–8…» (tester оновив).
  - coder / tester: ADR 002 «Уточнення реалізації» — `ContentValidator` відповідає за правила 1–8. → головна сесія виправила на «1–8 і 10».
  - tester, «Зауваження»: `HTTPS://…` проходить, `https://a` і `https://a/` не повтор (ordinal) — обидва варіанти коректні за карткою; U+200B у URL валідатор не ловить, закривається перевіркою живості `curl`.

### PRP-018 · Контент .NET, пачка 1: `dotnet-async`, `dotnet-clr`, `aspnet-core` по 12 питань
- **Статус:** done
- **Виконавець:** content-author
- **Пріоритет:** P1
- **Область:** content
- **Залежить від:** рішення 004 (accepted), PRP-017, файл `backend/.claude/agents/content-author.md` і `WebFetch` у `tester.md` (створює головна сесія після затвердження, проєкт в ADR 004, розділ 6). Якщо власник вирішить, що контент пише coder (питання 2 ADR 004), PM переписує картку під coder.
- **Навіщо:** у тесті .NET 7 із 10 тем зараз показують «0 питань». Async, пам'ять і GC, ASP.NET Core питають майже на кожній .NET-співбесіді, тож саме тут людина найбільше хоче знайти свої слабкі місця. Після картки тест .NET з одним рівнем дає до 24 питань із 6 тем замість 12 із 3.
- **Що зробити** (у `content/it/topics/`):
  1. `dotnet-async.yaml`, `dotnet-clr.yaml`, `aspnet-core.yaml` — по 12 питань: 4 `junior`, 4 `middle`, 4 `senior`, id від `<тема>-001` до `<тема>-012`.
  2. **Покриття** за колонкою «Покриває» spec 4.2, кожен пункт — щонайменше одне питання в темі:
     - `dotnet-async`: Task, async/await, SynchronizationContext, lock, TPL, Channels, дедлоки;
     - `dotnet-clr`: value/reference types, stack/heap, GC і покоління, IDisposable, Span;
     - `aspnet-core`: middleware, DI і lifetimes, маршрутизація, Minimal API, автентифікація, конфігурація.
  3. **Межі тем, без дублів:**
     - async-поведінка лише в `dotnet-async`;
     - життєвий цикл `DbContext` уже є в `ef-core-002`, тож питання про lifetimes в `aspnet-core` беруть інші сервіси;
     - відкладене виконання LINQ уже є в `csharp-005` і `csharp-006`.
  4. Усе за рішенням 004: рубрика (розділ 2), `sources` (розділ 3), чекліст (розділ 4), база .NET 10 / C# 14 (розділ 5).
     - Джерела — `https://learn.microsoft.com/en-us/…` або `https://devblogs.microsoft.com/dotnet/…`.
     - Кожне питання з `code`, яке можна запустити консольним file-based app (`dotnet run <файл>.cs` у scratchpad), перевірити запуском і показати вивід у звіті. Код, якому потрібен вебхост, — за джерелом, з позначкою у звіті.
  5. **Баланс:** `multiple` — від 3 до 5 у кожній темі. Блок `code` — щонайменше 3 у `dotnet-async` і `dotnet-clr`, щонайменше 2 в `aspnet-core`.
  6. Наприкінці — `validate`, потім `import` у dev-БД через `ConnectionStrings__Prepround`, щоб власник одразу побачив питання в запущеному застосунку.
- **Дозволені пакети:** нових немає (код не змінюється).
- **Критерії приймання** (команди з `backend/`, якщо не сказано інше):
  - [ ] `dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → `… тем 61, питань 72` і `0`
  - [ ] `import` (як у PRP-017) → `0` і `Питання: додано 36, оновлено 0, деактивовано 0` (наявні питання не змінено)
  - [ ] `curl -sk https://localhost:7120/api/catalog | jq -c '.fields[0].topics[] | select(.id=="dotnet-async" or .id=="dotnet-clr" or .id=="aspnet-core") | .questionCounts'` → тричі `{"junior":4,"middle":4,"senior":4}`
  - [ ] з кореня репо: `grep -c "type: multiple"` у кожному з 3 файлів → від `3` до `5`; `grep -c "^    code:"` → ≥ `3` для `dotnet-async.yaml` і `dotnet-clr.yaml`, ≥ `2` для `aspnet-core.yaml`
  - [ ] перевірка живості URL з PRP-017 на цих 3 файлах → порожньо і `1`; фільтр доменів `grep -vE '^https://(learn\.microsoft\.com/en-us/|devblogs\.microsoft\.com/dotnet/)'` → порожньо
  - [ ] звіт content-author містить таблицю з ADR 004 (розділ 6) для всіх 36 питань і вивід запуску для кожного питання з `code`, яке позначено як запущене
- **Для tester:**
  - фактчек за ADR 004 (розділ 6): усі питання з `code` — запуском; усі `senior` і всі `multiple` — за сторінкою з `sources`; з решти — щонайменше третина. Хибна правильна відповідь — `critical`, FAIL;
  - дублікати з `csharp` і `ef-core` (п. 3), а також всередині пачки;
  - рівні відповідають рубриці: вибірково по 2 питання кожного рівня. `junior` не вимагає знань `senior`, а `senior` складний розумінням, а не рідкісною назвою API;
  - порахувати, у скількох питаннях правильний варіант найдовший. Більше половини — зауваження для автора (не FAIL);
  - `csharp.yaml`, `ef-core.yaml`, `sql.yaml` і `field.yaml` не змінено: це доводить рядок імпорту `оновлено 0`.
- **Контракт API:** без змін.
- **Поза скоупом:** інші теми, зокрема `azure` (пізніша картка); правки наявних питань (помилки — у звіт); `field.yaml`; код і тести.
- **Після PASS — зупинка калібрування** (дорожня карта, етап 3): власник читає 10 випадкових питань пачки до того, як почнеться PRP-019.
- **Результат (2026-11-15, головна сесія):** PASS з 1 кола (content-author перервався на ліміті API й продовжив). 36 питань (`dotnet-async`, `dotnet-clr`, `aspnet-core` по 4/4/4), усього 72; `sources` у всіх, 50 URL живі, 40 якорів існують. Tester: 14/15 блоків `code` власним запуском, усі 12 `senior` і 10 `multiple` — WebFetch за джерелами; хибних відповідей немає. Тести бекенду 307/307 (tester зробив `ImportCommandTests`/`TestsEndpointTests` стійкими до росту контенту). **Зупинка калібрування.**
- **Хвости:**
  - content-author, «Помічено поза скоупом»: file-based app у .NET 10 за замовчуванням має `PublishAot=true` — для вебприкладів потрібен `#:property PublishAot=false`; JWT — `#:package Microsoft.AspNetCore.Authentication.JwtBearer@10.0.0` (лише scratchpad).
  - tester, «Зауваження» 1: картка контенту ламала 4 наявні тести (зашита кількість 36); автор цього не бачив, бо `dotnet test` не входить у його критерії. → головна сесія додала `dotnet test` у перевірку `content-author.md` і примітку в PRP-019.
  - tester, «Зауваження» 2: калібрування: clr-006 (`012`, покоління) ближче до `junior`; clr-011 (поріг `stackalloc`) ближче до `middle`. → на рішення власника.
  - tester, «Зауваження» 3: схожі пари без дубля: clr-005 / `csharp-010`; async-009 / async-010.
  - tester, «Зауваження» 4: пояснення clr-009 «LOH за замовчуванням не ущільнює» — коректно, але без нюансу про автоматичне ущільнення при hard limit (контейнер).

### PRP-019 · Контент .NET, пачка 2: `architecture`, `messaging`, `docker-k8s` по 12 питань
- **Статус:** dropped (2026-11-15: теми `architecture`, `messaging`, `docker-k8s` увійшли в хвилю 1, рішення власника про хвилі)
- **Виконавець:** content-author
- **Пріоритет:** P1
- **Область:** content
- **Залежить від:** PRP-018 і відгук власника на зупинці калібрування (можливі правки ADR 004 чи агента)
- **Навіщо:** це три спільні теми стеку .NET, які також входять у стеки Java, Go, PHP, Python і Node.js (spec 4.1). Одна пачка закриває порожні місця відразу в кількох стеках. Docker і Kubernetes, крім того, найближчі до DevOps-напряму власника.
- **Що зробити** (у `content/it/topics/`):
  1. `architecture.yaml`, `messaging.yaml`, `docker-k8s.yaml` — по 12 питань: 4 `junior`, 4 `middle`, 4 `senior`, id від `<тема>-001` до `<тема>-012`.
  2. **Покриття** за «Покриває» spec 4.2, кожен пункт — щонайменше одне питання:
     - `architecture`: SOLID, GoF-патерни, Clean Architecture, DDD, мікросервіси, system design;
     - `messaging`: Kafka, RabbitMQ, гарантії доставки, outbox, ідемпотентні консюмери;
     - `docker-k8s`: образи, шари, compose, поди, деплойменти, сервіси, probes, ресурси.
  3. **Нейтральність щодо стеку** (спільні теми ★): код лише SQL, YAML, Dockerfile, shell або псевдокод, без C#-специфіки.
  4. **Межі тем:**
     - Azure Service Bus, AKS, App Service — у `azure`;
     - REST, коди статусів, gRPC — в `api-design`;
     - outbox й ідемпотентність — у `messaging`, а не в `architecture`.
  5. **Однозначність в `architecture`:** питання про визначення й наслідки, які підтверджує джерело («що порушує цей код», «яку проблему розв'язує патерн»), а не «який патерн кращий».
  6. **Версії** (ADR 004, розділ 5): Kafka 4.4, RabbitMQ 4.4, Kubernetes 1.37, Docker Engine 29.9. Поведінку Dockerfile і compose, де можливо, перевірити запуском у локальному Docker 28.0.4. Kubernetes і брокери — за джерелом (кластера й брокера локально немає).
  7. **Джерела** за пріоритетом ADR 004 (розділ 3): docs.docker.com, kubernetes.io/docs, kafka.apache.org, rabbitmq.com/docs, learn.microsoft.com/en-us (Azure Architecture Center, .NET Architecture Guides), martinfowler.com і microservices.io — лише як факт.
  8. **Баланс:** `multiple` — від 3 до 5 у кожній темі; блок `code` — щонайменше 3 у `docker-k8s`.
  9. Наприкінці — `validate` і `import`, як у PRP-018.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] `validate` → `… питань 108` і `0`; `import` → `0` і `Питання: додано 36, оновлено 0, деактивовано 0`
  - [ ] `/api/catalog`: `questionCounts` для `architecture`, `messaging`, `docker-k8s` → тричі `{"junior":4,"middle":4,"senior":4}`
  - [ ] `grep -c "type: multiple"` у кожному з 3 файлів → від `3` до `5`; `grep -c "^    code:" content/it/topics/docker-k8s.yaml` → ≥ `3`; `grep -nE "^      lang: csharp" content/it/topics/architecture.yaml content/it/topics/messaging.yaml content/it/topics/docker-k8s.yaml` → порожньо
  - [ ] перевірка живості URL (як у PRP-017) на 3 файлах → порожньо і `1`; фільтр доменів `grep -vE '^https://(docs\.docker\.com|kubernetes\.io|kafka\.apache\.org|www\.rabbitmq\.com|learn\.microsoft\.com/en-us|martinfowler\.com|microservices\.io)/'` → порожньо
  - [ ] звіт content-author — таблиця з ADR 004 (розділ 6) для всіх 36 питань
- **Для tester:** як у PRP-018. Додатково: у `architecture` немає питань, де правильність залежить від смаку («кращий», «правильніший» без критерію з джерела); у `docker-k8s` прикладів, запущених локально, вистачає для перевірки шарів і кешу збірки.
- **Контракт API:** без змін.
- **Поза скоупом:** `azure`, `api-design`, `nosql`; правки наявних питань; код.

### PRP-020 · Акаунти й доступи для деплою: GitHub, Neon, Render, Cloudflare
- **Статус:** done (2026-11-19; власник створив і проєкт Pages `prepround` → `https://prepround.pages.dev`, тобто крок 4 PRP-024 уже виконано)
- **Виконавець:** власник
- **Пріоритет:** P1
- **Область:** backend + frontend
- **Платформа:** web
- **Залежить від:** рішення 005
- **Навіщо:** без доступу платформ до репо Render і Pages не зберуть код, а без БД у Neon API не має чого віддавати. Усе це вимагає акаунтів власника, агент цього не зробить.
- **Що зробити:**
  1. ~~GitHub: репо `dmitryspain/Prepround` і `dmitryspain/PreproundUI` (фронт; локальна тека лишається `PreproundWeb`), перші коміти й push, гілка `master`.~~ Виконано.
  2. **Хвости першого коміту `Prepround`.** У нього потрапили сміттєві `backend/g.sh` і `backend/p.txt` (скрипт агента і ~650 КБ скопійованої сторінки документації). Секретів, `bin/` і `obj/` там немає, перевірено. Головна сесія вже видалила ці файли з диска. Лишилось закомітити й запушити:
     - їх видалення;
     - нові файли, яких досі немає в git: `backend/.claude/pm/content-brief.md` (бриф хвиль) і `backend/.claude/tools/` (`content-lint.py`);
     - ADR 005, оновлені беклог і knowledge.
  3. **Neon.** ~~Проєкт створено.~~ Перевірити в налаштуваннях проєкту:
     - регіон **AWS Europe Central 1 (Frankfurt)**. Регіон після створення не змінюється: якщо він інший, створити проєкт заново (дані не втрачаються, БД відтворюється з git);
     - версію Postgres: очікується 18. Версія 17 теж підходить: міграція `InitialCatalog` має лише `text`, `integer`, `boolean`, `jsonb`, `timestamptz` і жодного сирого SQL (перевірено PM 2026-11-19).

     Головній сесії написати регіон і версію, без рядків підключення.
  4. **Neon: створити БД `prepround`** (Databases → New database, власник — роль за замовчуванням `neondb_owner`). Більше нічого. Міграції, імпорт і роль для API — у PRP-024.
  5. **Render: дати GitHub App «Render» доступ до репо `dmitryspain/Prepround`**: GitHub → Settings → Applications → Installed GitHub Apps → Render → Configure → Repository access → додати `Prepround`. Сервіс поки **не створювати**: без Dockerfile (PRP-021) збірка впаде. Його створює PRP-024. Якщо Render попросить банківську картку, зупинитись і написати головній сесії: умова ADR 005 — без картки.
  6. **Cloudflare:** ~~акаунт (вхід через GitHub)~~. Перевірити, що в Workers & Pages → Create є вкладка **Pages** з «Import an existing Git repository» і що Cloudflare бачить репо `dmitryspain/PreproundUI`. Проєкт поки **не створювати**: до PRP-022 сайт ходив би на `localhost`. Якщо вкладки Pages немає, а є лише Workers, написати головній сесії (ADR 005, «Ризики»).
  7. **За бажанням: `docker builder prune`.** Звільняє ~21 ГБ кешу збірки (на `/System/Volumes/Data` вільно 16 ГБ, зайнято 97 %, 2026-11-19). Збірки PRP-021 потребують ~2–3 ГБ. Ціна: інші проєкти перезбиратимуться довше один раз.
  8. **Рядки підключення Neon у чат не вставляти й агентам не передавати.** Сценарій Neon для агентів (`npm i -g neon`, `neon init`/`neon deploy`, файли `neon.ts` і `.env.local`) не використовуємо (ADR 005, «Відкинуто»).
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [x] `git -C /Users/dmitryspain/RiderProjects/Prepround remote -v` → `https://github.com/dmitryspain/Prepround.git`; `git -C /Users/dmitryspain/RiderProjects/PreproundWeb remote -v` → `https://github.com/dmitryspain/PreproundUI.git`; гілка `master` в обох (PM, 2026-11-19)
  - [ ] `git -C /Users/dmitryspain/RiderProjects/Prepround status --short backend/g.sh backend/p.txt backend/.claude` → порожньо, а `git -C /Users/dmitryspain/RiderProjects/Prepround status -sb` не показує `ahead`
  - [ ] власник написав регіон (Frankfurt) і версію Postgres у Neon; БД `prepround` існує
  - [ ] у формі створення сервісу Render у списку репо є `dmitryspain/Prepround`
  - [ ] у Cloudflare є вкладка Pages, а в списку репо — `dmitryspain/PreproundUI`
- **Виконано (2026-11-19):** п. 1 — репо, перші коміти й push в обох, гілка `master`; п. 3 — проєкт Neon створено, власник підтвердив: **Frankfurt, Postgres 18**; п. 6 — акаунт Cloudflare; акаунт Render. П. 2 частково: коміт `7746f5d` видалив `g.sh` і `p.txt` (запушено).
- **Лишилось:**
  - п. 2 — нові файли (`content-brief.md`, `tools/`, ADR 005, беклог, knowledge) ще не в git. Головна сесія запропонує їх разом із комітами PRP-021 і PRP-023, після PASS PRP-023: зараз coder змінює ті самі файли;
  - ~~п. 4 — БД `prepround`~~ — створено (власник, 2026-11-19);
  - ~~п. 5 — доступ Render до репо~~ — Render бачить `Prepround` (власник, 2026-11-19);
  - п. 6 — перевірити Pages;
  - п. 7 — за бажанням.
- **Для tester:** не стосується (картка власника).
- **Контракт API:** без змін.
- **Поза скоупом:** створення сервісу Render і проєкту Pages, міграції, імпорт і роль читача (PRP-024); Neon CLI; домен prepround.app («Ідеї»).

### PRP-021 · Docker-образ API і робота за проксі Render
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** рішення 005 (розділи 2–4)
- **Навіщо:** Render Free запускає API лише з Docker-образу. За проксі Render і Cloudflare всі запити приходять з внутрішньої IP проксі, тож без справжньої IP клієнта rate limit `tests` (30 на хвилину з однієї IP, spec 5.2) став би одним на всіх: 31-й тест за хвилину від будь-кого отримав би `429`. А в образі без ICU змінився б порядок тем у каталозі (хвіст PRP-005).
- **Що зробити** (шляхи від `Prepround/`):
  1. **`backend/Dockerfile`, multi-stage.** Це вимога, а не дослівний текст: якщо щось нижче не працює, поясни у звіті, що змінив і чому.
     - **Збірка** на `mcr.microsoft.com/dotnet/sdk:10.0`:
       - спершу копіюються лише `global.json`, `nuget.config`, `Directory.Build.props`, `Directory.Packages.props` і `*.csproj` з `src/`, потім `dotnet restore src/Prepround.Api/Prepround.Api.csproj`. Окремий шар кешується між збірками;
       - потім решта `src/` і `dotnet publish src/Prepround.Api/Prepround.Api.csproj -c Release -o /app --no-restore`.

       Framework-dependent, без RID і без `--platform`: Render збирає на своїй архітектурі, а tester — на arm64.
     - **Запуск** на `mcr.microsoft.com/dotnet/aspnet:10.0`. Це Ubuntu 24.04 з ICU. Не `-alpine` і не `-chiseled`: там немає ICU, і `StringComparer.InvariantCulture` поміняв би порядок тем.
     - `ENV ASPNETCORE_ENVIRONMENT=Production`, `USER $APP_UID` (в образі це 1654, тобто не root), `EXPOSE 8080`.
     - **Порт:** застосунок слухає `${PORT:-8080}`, наприклад `ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Prepround.Api.dll"]`. Render передає `PORT` (за замовчуванням 10000), локально без `PORT` — 8080. Інших способів не додавати: ні коду в `Program.cs`, ні `ASPNETCORE_URLS`.
  2. **`backend/.dockerignore`**, щонайменше: `**/bin/`, `**/obj/`, `.claude/`, `.idea/`, `.config/`, `tests/`, `**/*.user`, `docker-compose.yml`, `**/appsettings.Development.json`. У прод-образі не має бути навіть локального dev-рядка підключення.
  3. **Forwarded headers** у `backend/src/Prepround.Api/Program.cs`:
     - `ForwardedHeadersOptions`:
       - `ForwardedHeaders = XForwardedFor | XForwardedProto`;
       - `KnownProxies.Clear()` і `KnownIPNetworks.Clear()`, а не `KnownNetworks`: у .NET 10 він obsolete (ASPDEPR005), і `TreatWarningsAsErrors` зламає збірку. Коментар англійською пояснює чому: Render не публікує IP своїх проксі, а дістатися до контейнера можна лише через них;
     - `ForwardedForHeaderName` і `ForwardLimit` читаються з секції конфігурації `ForwardedHeaders`. Без неї діють стандартні значення: `X-Forwarded-For` і `1`. Так їх можна змінити змінною Render без нової збірки, наприклад `ForwardedHeaders__ForwardLimit=2`;
     - `app.UseForwardedHeaders()` стоїть першим у конвеєрі, до `UseHttpsRedirection`, `UseCors` і `UseRateLimiter`;
     - `RateLimitPolicies` не змінювати: він, як і раніше, ділить за `RemoteIpAddress`, а тепер цю адресу виставляє middleware.
  4. **`backend/src/Prepround.Api/appsettings.Production.json`** (новий):
     - `Cors:Origins` = `["https://prepround.pages.dev"]`;
     - `ForwardedHeaders:ForwardedForHeaderName` = `CF-Connecting-IP`. У `X-Forwarded-For` на Render найправіший запис — IP вузла Cloudflare, а ліворуч стоїть будь-що, що надіслав клієнт. `CF-Connecting-IP` містить одну IP клієнта (ADR 005, розділ 4);
     - `Logging:LogLevel:Microsoft.EntityFrameworkCore` = `Warning`, інакше кожен SQL-запит іде в лог Render.
  5. **`http://localhost:4300`** перенести з `Cors:Origins` у `appsettings.json` до `appsettings.Development.json`. Масиви конфігурації зливаються за індексом, тож origin з базового файлу не має потрапити в Production. В `appsettings.json` лишається порожній масив `Cors:Origins` або секції немає зовсім.
  6. **`Prepround/README.md`**, розділ «Docker-образ API»: збірка й локальний запуск з `backend/` (команди з критеріїв нижче) і що Render передає `PORT`.
- **Дозволені пакети:** нових немає (`ForwardedHeaders` входить у shared framework `Microsoft.AspNetCore.App`).
  - **Docker-образи — виняток з allowlist хвиль у CLAUDE.md лише для цієї картки:** coder і tester можуть робити `docker pull` образів `mcr.microsoft.com/dotnet/sdk:10.0` і `mcr.microsoft.com/dotnet/aspnet:10.0`. Обидва вже є локально, але amd64 і застарілі (runtime 10.0.11), тож pull оновить їх до arm64-варіанта з актуальним патчем.
  - Свої образи називати `prepround-api:prp021`, а контейнери — `prepround-api-prp021*`.
- **Критерії приймання** (команди з `Prepround/backend`):
  - [ ] `docker build -t prepround-api:prp021 .; echo $?` → `0`
  - [ ] `docker run --rm --entrypoint sh prepround-api:prp021 -c 'id -u; ls /app | grep -c appsettings.Development.json'` → `1654` і `0`
  - [ ] **запуск проти dev-БД.** Команда: `docker run -d --name prepround-api-prp021 -p 18080:8080 -e "ConnectionStrings__Prepround=Host=host.docker.internal;Port=5433;Database=prepround;Username=prepround;Password=prepround" prepround-api:prp021`. Очікувано:
    - `curl -s http://localhost:18080/api/health` → `{"status":"ok"}`;
    - `curl -s -o /dev/null -w '%{http_code}' http://localhost:18080/api/catalog` → `200`;
    - `curl -s -o /dev/null -w '%{http_code}' http://localhost:18080/openapi/v1.json` → `404` (Production).
  - [ ] **порт.** Той самий образ з `-e PORT=10000 -p 18081:10000` → `curl -s http://localhost:18081/api/health` → `{"status":"ok"}`
  - [ ] **порядок тем.** `curl -s http://localhost:18080/api/catalog | jq -c '[.fields[0].topics[].id]'` дорівнює виводу тієї самої команди для `https://localhost:7120/api/catalog` (з `-k`). Якщо локального API немає, tester запускає його сам і зупиняє наприкінці
  - [ ] **CORS у контейнері (Production).**
    - `curl -si -X OPTIONS http://localhost:18080/api/tests -H 'Origin: https://prepround.pages.dev' -H 'Access-Control-Request-Method: POST' -H 'Access-Control-Request-Headers: content-type'` → `Access-Control-Allow-Origin: https://prepround.pages.dev`;
    - те саме з `Origin: http://localhost:4300` → заголовка немає.
  - [ ] **rate limit у контейнері.**
    - 30 × `curl -s -o /dev/null -w '%{http_code}' -X POST http://localhost:18080/api/tests -H 'Content-Type: application/json' -d '{}' -H 'CF-Connecting-IP: 203.0.113.1' -H "X-Forwarded-For: 198.51.100.$i"` → жодного `429`;
    - 31-й з тією самою `CF-Connecting-IP` → `429`;
    - одразу після цього запит з `CF-Connecting-IP: 203.0.113.2` → не `429`.
  - [ ] **перевизначення змінними (запасний варіант ADR 005).** Контейнер з `-e ForwardedHeaders__ForwardedForHeaderName=X-Forwarded-For -e ForwardedHeaders__ForwardLimit=2` ділить ліміт за другим записом справа:
    - 30 запитів з `X-Forwarded-For: <випадкова>, 203.0.113.7, 104.16.0.1` → 31-й → `429`;
    - запит з `X-Forwarded-For: <випадкова>, 203.0.113.8, 104.16.0.1` → не `429`.
  - [ ] `dotnet build` → 0 попереджень; `dotnet test; echo $?` → `0`, `failed: 0`. Охоронні тести зелені: `CorsTests`, `StartupWithoutDatabaseTests`, `TestsEndpointTests` (rate limit з `RemoteIpAddress`), `HandlerCompositionTests`, `LayerDependencyTests`
  - [ ] **нові автотести tester** через `ApiFactory`:
    - Development без заголовків поводиться як досі;
    - `ApiFactory.Create("Production")`: CORS пропускає лише `https://prepround.pages.dev`, ліміт ділиться за `CF-Connecting-IP`, різні `X-Forwarded-For` з однаковим `CF-Connecting-IP` нових розділів не створюють.
  - [ ] **прибирання:** `docker ps -a --filter name=prepround-api-prp021 -q` і `docker images prepround-api -q` → порожньо
- **Для tester:**
  - Перед збіркою `df -h /System/Volumes/Data`: якщо вільно < 5 ГБ, поверни BLOCKED. Урок 2026-11-15: заповнений диск ламав образи.
  - `{}` у тілі дає `400`, але дозвіл ліміту однаково витрачає: limiter стоїть перед ендпоінтом.
  - `http://` у контейнері не дає `307`: у Production без HTTPS-порту `UseHttpsRedirection` лише раз пише попередження.
  - **Для звіту, не критерій** (портфоліо, ADR 005, «Метрики»): розмір образу (`docker images`), час холодної й повторної збірки. Повторна збірка після зміни одного `.cs` має взяти restore з кешу.
  - **За бажанням:** `docker build --platform linux/amd64` (архітектура Render). Якщо емуляція падає, це примітка, а не FAIL.
  - Не видаляти образів, які були до картки: `sdk:10.0`, `sdk:10.0-alpine`, `aspnet:10.0`, `aspnet:10.0-noble` та інших проєктів власника. `docker builder prune` не запускати: це кеш інших проєктів.
- **Контракт API:** без змін. «Одна IP-адреса» зі spec 5.2 за проксі тепер означає IP клієнта з `CF-Connecting-IP`.
- **Поза скоупом:**
  - створення сервісу Render (PRP-024);
  - міграції на старті;
  - `/api/health` з перевіркою БД і `MapHealthChecks`: health лишається liveness без БД (spec 5.3), на ньому health check Render;
  - зміни `RateLimitPolicies` і ліміту;
  - стиснення, CI, сканування, pin за digest («Ідеї»);
  - `capacitor://localhost` (PRP-008).

**Результат (2026-11-19):** PASS з 1 кола; тести: total 316 / failed 0 (308 наявних + 8 нових у `tests/Prepround.Tests/Api/ForwardedHeadersTests.cs`). Образ 297 МБ (`/app` — 8,4 МБ). Холодна збірка 13–19 с, після зміни одного `.cs` — 6 с, restore з кешу.
**Хвости:**
- **coder, «Помічено поза скоупом»:**
  - У лозі контейнера при новому з'єднанні з Postgres з'являється `Cannot load library libgssapi_krb5.so.2 ... No such file or directory`. Це, найімовірніше, спроба шифрування GSS, яку Npgsql за замовчуванням вмикає в режимі `Prefer`. Запит після цього проходить, каталог віддає 200. Виправити можна двома способами: додати `GSS Encryption Mode=Disable` у рядок підключення для Render (ADR 005, розділ 5) або поставити `libgssapi-krb5-2` в образ. Я не робив ні того, ні іншого.
  - Після `docker pull` старий amd64 `mcr.microsoft.com/dotnet/sdk:10.0` (id `cf3e50b742c6`, 887 МБ) лишився без тегу (`<none>`). Старий aspnet (`9bdfb7d1291d`) і далі має тег `10.0-noble`. Картка забороняє видаляти образи, що були до неї, тож я їх не видаляв. Видалити `<none>` чи ні, вирішує головна сесія або власник.
- **tester, «Зауваження»:**
  - `docker build --platform linux/amd64` (за бажанням) я не робив. Для нього довелося б тягнути amd64-варіанти базових образів. У класичному сховищі образів це перевісило б теги `sdk:10.0` і `aspnet:10.0` назад на amd64 і скасувало б щойно зроблений arm64 pull. Архітектура Render перевіриться збіркою в PRP-024.
  - `Program.cs`, `forwardedHeaders.GetValue("ForwardLimit", options.ForwardLimit)`, тип `int?`. Це я вивів із коду `ConfigurationBinder` і запуском не перевіряв: порожнє значення змінної (`ForwardedHeaders__ForwardLimit=`), найімовірніше, дасть `ForwardLimit = null`, тобто без ліміту. З `CF-Connecting-IP` це нешкідливо, бо там один запис. У запасному режимі з `X-Forwarded-For` переміг би найлівіший запис, який задає клієнт. Досить пам'ятати в PRP-024, що змінну задають лише непорожнім числом.
  - У Development тепер теж діє `X-Forwarded-For` з лімітом 1 (довірених проксі немає), тож у dev IP можна підробити заголовком. Coder про це писав. Для локальної розробки це прийнятно, на Production не впливає: тест `Production_ignores_x_forwarded_for…`.
  - Restore-шар Dockerfile перелічує 4 csproj явно (припущення coder). Нове посилання API на інший проєкт зламає restore в образі помітно, а не мовчки. Це прийнятно.
  - Цифру «309 тестів» у беклозі краще виправити на 308: у HEAD 156 `[Fact]` + 152 `[InlineData]` = 308, пропущених немає, а тести з першого коміту не змінювались. Звідки була 309, з git уже не з'ясувати.
- **Примітка головної сесії:** `GSS Encryption Mode=Disable` і «`ForwardLimit` лише непорожнім числом» → примітки до PRP-024 (рядки підключення й змінні Render). Видалення образу `<none>` (887 МБ) — на розсуд власника.

### PRP-022 · Продакшн-збірка фронту для Cloudflare Pages і підказка про холодний старт API
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** рішення 005 (розділи 1–2). Від PRP-021 не залежить: контракт той самий
- **Навіщо:**
  - Зараз будь-яка збірка ходить на `https://localhost:7120/api`, тож сайт на Pages не зміг би отримати каталог.
  - API на Render Free після 15 хв простою прокидається близько хвилини, а цей час людина бачить лише «Завантаження…». У MVP так зустрічатиме майже кожного першого відвідувача, і він вирішить, що сайт зламаний.
- **Що зробити** (репо `PreproundWeb`, шляхи від його кореня):
  1. **`src/environments/environment.production.ts`:** `apiBaseUrl: 'https://prepround-api.onrender.com/api'`, коментар про `fileReplacements` поруч. Якщо назва сервісу на Render інша, головна сесія змінить цей рядок у PRP-024. `src/environments/environment.ts` не змінювати: dev і тести лишаються на `https://localhost:7120/api`.
  2. **`angular.json`:** у `build.configurations.production` додати `fileReplacements`: `src/environments/environment.ts` → `src/environments/environment.production.ts`. Нових конфігурацій не додавати.
  3. **SPA-фолбек: нічого не додавати.** Pages віддає `index.html` на будь-який шлях, якщо в корені виходу немає `404.html` (документація Pages, «Serving Pages»). `_redirects` і `404.html` не створювати.
  4. **Node на Pages** береться з наявного `.node-version` (22.23.5): збірка Pages v3 читає цей файл, а за замовчуванням ставить 22.23.4. `NODE_VERSION` не потрібен, файл не змінювати.
  5. **Підказка в `src/app/setup/`.** Якщо каталог вантажиться довше 5 с, під «Завантаження…» з'являється «Сервер прокидається після простою — це може тривати до хвилини.».
     - Коли каталог прийшов або сталася помилка, підказка зникає разом із «Завантаження…».
     - Поріг — іменована константа (`core/config.ts` або поруч із компонентом).
     - Таймер через RxJS `timer` або `setTimeout`, прибирається при знищенні компонента.
     - Колір лише через токени (`var(--…)`, клас `.muted`).
  6. **`README.md`, розділ «Деплой»:**
     - Cloudflare Pages: проєкт `prepround`, репо GitHub `dmitryspain/PreproundUI`, гілка `master`;
     - збірка: команда `npm run build`, вихідна тека `dist/web/browser`, Node з `.node-version`;
     - адреса API для проду лежить у `src/environments/environment.production.ts`.
- **Дозволені пакети:** нових немає.
- **Критерії приймання** (з `/Users/dmitryspain/RiderProjects/PreproundWeb`):
  - [ ] `npm run build; echo $?` → `0`, у виводі 0 попереджень
  - [ ] `grep -l 'https://prepround-api.onrender.com/api' dist/web/browser/*.js | wc -l` → ≥ `1`; `grep -rl 'localhost:7120' dist/web/browser | wc -l` → `0`
  - [ ] `npx ng build --configuration development --output-path <scratchpad>/dev-build; echo $?` → `0`; у `<scratchpad>/dev-build/browser/*.js` є `localhost:7120` і немає `onrender.com`
  - [ ] `test -f dist/web/browser/index.html && test -f dist/web/browser/theme-init.js && test ! -e dist/web/browser/404.html && test ! -e dist/web/browser/_redirects; echo $?` → `0`
  - [ ] `grep -rln 'onrender' src --include='*.ts'` → лише `src/environments/environment.production.ts`
  - [ ] **тести Setup з fake timers** (Vitest `vi.useFakeTimers()` або рівнозначно) і фейковим каталогом, що не відповідає:
    - через 4,9 с підказки немає;
    - через 5 с є;
    - після відповіді каталогу підказки й «Завантаження…» немає;
    - після помилки каталогу підказки немає.
  - [ ] `npx ng test --watch=false; echo $?` → `0`, failed 0
- **Для tester:**
  - **Тести й API.** Перевір, чи `ng test` не підхоплює продакшн-адресу. Тести, що спираються на `API_BASE_URL`, мають задавати його самі, як `setup.spec.ts` і `api.service.spec.ts`.
  - **Вигляд підказки.** На 390 px вона не дає горизонтальної прокрутки. Скриншот у трьох темах — у звіт, не критерій.
  - **Бюджети.** `anyComponentStyle` не перевищено. Найбільший стиль — `test-run.scss`, а Setup теж близький до межі.
- **Контракт API:** без змін.
- **Поза скоупом:**
  - CSP і `_headers` («Ідеї»);
  - Workers Static Assets, keep-alive, домен;
  - зміна `environment.ts`;
  - `withViewTransitions()`;
  - iOS (`environment.mobile.ts`, `dist/mobile`, PRP-008);
  - підказка на інших екранах: на момент `POST /api/tests` сервер уже прокинувся.

**Результат (2026-11-19):** PASS з 1 кола; тести: `PreproundWeb` total 201 / failed 0 (193 наявних + 8 нових у `setup.spec.ts` і `core/config.spec.ts`). Прод-збірка без попереджень, initial 348.74 kB. Підказку на 390 px перевірено в трьох темах, горизонтальної прокрутки немає.
**Хвости:**
- **coder, «Помічено поза скоупом»:**
  - `PreproundWeb/angular.json` не проходить `prettier --check` і до моєї зміни: в `options.styles` масив розбито на кілька рядків, а prettier хоче `["src/styles.scss"]`. Мій блок `fileReplacements` відформатовано за prettier. Існуючий рядок не чіпав.
  - `PreproundWeb/CLAUDE.md` (рядок 23) і розділ «Фронтенд» у `Prepround/backend/.claude/CLAUDE.md` кажуть, що значення адреси API лежить у `src/environments/environment.ts`. Тепер для продакшн-збірки воно в `src/environments/environment.production.ts`. Варто дописати при закритті картки (п. 5 «Погнали»: зміна структури).
- **tester, «Зауваження»:**
  - `src/app/setup/setup.spec.ts` не проходив `prettier --check` ще до цієї картки: перевірено на `HEAD`, причина — довгі рядки `expect(...).toBe("Не вдалося зв'язатися із сервером.")`. Мій новий рядок з alert написано в тому самому стилі. Якщо колись форматуватимуть увесь файл, `prettier --write` виправить і його.
  - SPA-фолбек Pages без `404.html` перевірено лише через вміст виходу. Що прямий захід на `/test` або `/settings` віддає `index.html`, видно тільки на живому сайті, тож це варто перевірити в PRP-024 (наприклад, `curl -s -o /dev/null -w '%{http_code}' https://prepround.pages.dev/settings` → 200).
  - «Завантаження…» і нова підказка не мають `aria-live`, тож скрінрідер не оголосить підказку, яка з'явиться через 5 с. Для MVP некритично, кандидат в «Ідеї».
- **Головна сесія:** CLAUDE.md обох репо оновлено (`environment.production.ts`). SPA-фолбек на живому сайті перевіряється smoke-тестом PRP-024 (критерій `/result` → 200). Prettier у двох файлах і `aria-live` → «Ідеї», дрібниці.

### PRP-023 · Чернетки тем: `draft: true` і `import` без чернеток за замовчуванням
- **Статус:** done
- **Виконавець:** coder
- **Пріоритет:** P1 (блокує перший імпорт у прод: без неї туди потрапить відома хибна відповідь)
- **Область:** backend + content
- **Залежить від:** рішення 005 (розділ 6), рішення 002 (розділ «Уточнення: чернетки тем»)
- **Навіщо:** у `content/` зараз 84 питання з 7 тем без PASS фактчеку. У `pinia-003` фактчек уже знайшов хибну «правильну» відповідь (`critical`). `import` бере всю теку, тож перший імпорт у Neon показав би людям неперевірені питання, а одне з них свідомо хибне. Фактчек ловив `critical` приблизно в кожній шостій темі (`sql-010`, `laravel-010`, `php-lang-012`, `pinia-003`). Позначка у файлах надійніша за список у пам'яті головної сесії. Хвилі тривають, а імпорт у прод запускає власник.
- **Що зробити** (шляхи від `Prepround/`):
  1. **Формат.** Необов'язкове поле теми `draft` (`true` / `false`, за замовчуванням `false`) на верхньому рівні файлу теми, поруч з `id`, `title`, `description`.
     - Читає `backend/src/Prepround.Infrastructure/Content/YamlContentReader.cs`. Небулеве значення — помилка читача з файлом і рядком, як для `correct`.
     - `TopicDefinition` отримує `bool IsDraft` або рівнозначне поле.
     - `validate` перевіряє чернетки, як і решту тем. У рядку підсумку додається кількість питань у чернетках, наприклад «… питань 648, з них у чернетках 84».
  2. **`import <тека>` за замовчуванням не імпортує питань тем-чернеток.** Вони вважаються відсутніми у файлах (рішення 002, «Семантика імпорту»): нові не додаються, раніше імпортовані стають `IsActive = false`. Сама тема (назва, опис, місце в стеках) імпортується як завжди.
     - `import <тека> --include-drafts` імпортує все, як зараз.
     - Фільтр живе в одному місці в Application: прапорець у `ImportContentCommand` і його обробка в `ImportContentHandler` або рівнозначно. Не в ContentTool. Обидва режими йдуть через той самий `ContentLoader`.
  3. **ContentTool.** Форма `import <тека> [--include-drafts]`, прапорець описано в Usage. Інший прапорець або інший порядок аргументів → код `2` і повідомлення.
  4. **Контент: `draft: true` у 14 темах.** Перед передачею coder головна сесія звіряє перелік із «Хвилями контенту». Якщо якась із 7 тем з питаннями вже отримала PASS, `draft` їй не ставлять, а очікувані числа в критеріях зсуваються на 12.
     - **7 тем з питаннями без PASS на 2026-11-18:** `pinia`, `angular-di`, `nextjs`, `react-state`, `angular-change-detection`, `vue-router`, `react-data-routing`;
     - **7 порожніх тем:** `html-css`, `frontend-testing`, `web-performance`, `rxjs`, `ngrx`, `angular-routing-forms`, `nuxt`. Автор, який їх наповнить, з першого рядка працюватиме в чернетці.

     Інших змін у контенті немає.
  5. **`Prepround/README.md`:** у розділі ContentTool — рядок про `draft` і `--include-drafts`: dev-БД імпортує з прапорцем, прод без нього.
- **Дозволені пакети:** нових немає.
- **Критерії приймання** (з `Prepround/backend`). Числа — на контенті 2026-11-19: 648 питань, 84 з них у 7 чернетках. БД тимчасова, щоб не чіпати dev-БД власника:
  - [ ] `dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → `0`, у підсумку є `648` і `84`
  - [ ] `grep -l '^draft: true' ../content/it/topics/*.yaml | wc -l` → `14`, перелік збігається з п. 4; `git diff --stat -- ../content` → лише рядки `draft` у 14 файлах
  - [ ] **зламана копія** в scratchpad: `draft: maybe` в одній темі → `validate` → `1` і рядок із файлом цієї теми
  - [ ] **тимчасова БД:**
    1. `docker exec prepround-postgres createdb -U prepround prepround_prp023` → `0`;
    2. `dotnet ef database update --connection "Host=localhost;Port=5433;Database=prepround_prp023;Username=prepround;Password=prepround" --project src/Prepround.Infrastructure --startup-project src/Prepround.Api` → `0`;
    3. `ConnectionStrings__Prepround=<той самий рядок> dotnet run --project src/Prepround.ContentTool -- import ../content` → `0`, `Питання: додано 564, оновлено 0, деактивовано 0`;
    4. `docker exec prepround-postgres psql -U prepround -d prepround_prp023 -tAc 'SELECT count(*) FROM "Questions" WHERE "IsActive"'` → `564`, а `SELECT count(*) FROM "Topics" WHERE "IsActive"` → `61`.
  - [ ] **на тій самій БД:**
    - `import ../content --include-drafts` → `0`, `Питання: додано 84, оновлено 0, деактивовано 0`, активних `648`;
    - знову `import ../content` → `Питання: додано 0, оновлено 0, деактивовано 84`, активних `564`. Це шлях аварійного вимкнення теми.
  - [ ] `import ../content --bogus; echo $?` → `2`; `import --include-drafts ../content; echo $?` → `2`
  - [ ] **прибирання:** `docker exec prepround-postgres dropdb -U prepround prepround_prp023` → `0`. Кількість активних питань у dev-БД `prepround` така сама, як до картки
  - [ ] `PREPROUND_TEST_CONTENT_DIR=<знімок content> dotnet test; echo $?` → `0`, `failed: 0`.
    - Охоронні тести зелені: `ImportCommandTests`, `ValidateCommandTests`, `ValidateUnreadYamlTests`, `UsageTests`, `ContentToolConfigurationTests`, `LayerDependencyTests`, `HandlerCompositionTests`.
    - Тести, що порівнюють API з файлами, tester оновлює під новий режим і перевіряє той самий факт.
- **Для tester:**
  - **Чернетка в стеку.** Тема-чернетка, на яку посилаються стеки, не ламає `validate` і `import`. У `/api/catalog` її `questionCounts` за замовчуванням порожній або нульовий.
  - **`draft: false`** поводиться так само, як відсутнє поле.
  - **Ідемпотентність.** Повторний `import` у тому самому режимі дає `додано 0, оновлено 0, деактивовано 0`.
  - **`UpdatedAt` реактивованих.** Питання, яке повернулося з чернетки, оновлює `UpdatedAt` лише якщо змінився вміст (як у PRP-004).
  - **`content-lint.py`.** `python3 backend/.claude/tools/content-lint.py --all` не скаржиться на новий ключ. Якщо скаржиться, це зауваження для головної сесії: інструмент її.
- **Контракт API:** без змін. API, як і раніше, віддає лише активні питання; БД і spec 5 про `draft` не знають.
- **Поза скоупом:**
  - `draft` на рівні питання; нова колонка в БД чи міграція; зміни в API і фронті;
  - правки текстів питань;
  - зняття `draft` після PASS (головна сесія);
  - оновлення `content-brief.md`, `content-author.md` і CLAUDE.md (пропозиції PM, вносить головна сесія).

**Результат (2026-11-19):** PASS з 1 кола; тести: total 351 / failed 0 (на знімку контенту). Тимчасова БД: за замовчуванням 564 активних питання, з `--include-drafts` 648, повторний імпорт за замовчуванням деактивує 84. Мутації ключової логіки (3 штуки) тести ловлять.
**Хвости:**
- **coder, «Помічено поза скоупом»:**
  - `content-brief.md`, `content-author.md` і CLAUDE.md ще не знають про `draft` і `--include-drafts`. Зокрема, команда `import` у «Командах» CLAUDE.md і dev-імпорт хвиль ідуть без прапорця, тож dev-БД після наступного імпорту втратить питання чернеток. Це справа головної сесії, як і передбачає картка.
  - `import --include-drafts` порожніх тем-чернеток нічого не додає, і це нормально. Але `validate` не показує, скільки тем у чернетках, лише кількість питань.
- **tester, «Зауваження»:**
  - **Суфікс `, з них у чернетках N` лише при N > 0.** Я не вважаю це відхиленням: критерій «у підсумку є 648 і 84» на реальному контенті виконано, а приклад у картці описує саме випадок із чернетками. Без чернеток вивід лишається байт-у-байт старим, тож охоронні тести з точними рядками фікстур зелені. Тест реального контенту тепер перевіряє обидві гілки через сирі лічильники.
  - `draft: "true"` у лапках, як і `True` / `TRUE`, читається як `true`: `Flag` не дивиться на стиль скаляра. Так само поводиться й `correct`, тож поведінка узгоджена, але формально рядок у лапках — не YAML-булеве значення.
  - `validate` показує кількість питань у чернетках, але не кількість тем-чернеток (це помітив і coder). Сім порожніх тем-чернеток у підсумку не видно.
  - Рядок Usage `Prepround.ContentTool <команда> <тека> [прапорець]` можна прочитати так, ніби прапорець приймає й `validate`. Насправді `validate <тека> --include-drafts` → 2. Це дрібниця тексту.
  - Код картки потрапив у коміт `5db32fa` разом із файлами PRP-021, а тести з цієї перевірки й 14 рядків `draft` поки не закомічені. Власнику варто закомітити їх разом під PRP-023.
  - `Default_import_leaves_draft_questions_out…` на реальному контенті вироджується, коли в контенті не залишиться чернеток із питаннями. Тоді механізм покривають тести на копії (sql, ef-core) і юніт-тести, тож вимоги «є хоч одна чернетка» я свідомо не додавав.
- **Головна сесія:** бриф, `content-author.md` і CLAUDE.md оновлено 2026-11-19 (чернетки; dev-імпорт з `--include-drafts`; `draft` знімає головна сесія після PASS). Кількість тем-чернеток у `validate` і текст Usage → «Ідеї», дрібниці. Коміт `5db32fa` не містив нових файлів (`Dockerfile`, `.dockerignore`, `appsettings.Production.json`, `ForwardedHeadersTests.cs`, ADR 005, бриф, `tools/`): власник комітить їх разом із рештою перед PRP-024.

### PRP-024 · Деплой MVP: Neon, Render, Cloudflare Pages і перевірка живого сайту
- **Статус:** todo
- **Виконавець:** власник (кроки 1–5), головна сесія (кроки 6–7)
- **Пріоритет:** P1
- **Область:** backend + frontend
- **Платформа:** web
- **Залежить від:** PRP-020, PRP-021, PRP-022, PRP-023 — закомічені й запушені в `master` обох репо
- **Навіщо:** MVP стає доступним з будь-якого пристрою за посиланням. Власник може дати його людям, які готуються до співбесід, і отримати перший живий відгук.
- **Що зробити:**
  1. **Neon: схема і контент** (власник, термінал, з `Prepround/backend`).
     - Рядок: Neon → Connect → роль `neondb_owner`, БД `prepround`, **pooling вимкнено** (direct). Формат key-value, бо Npgsql не розуміє URI `postgresql://…`:

       `Host=<ep-…>.eu-central-1.aws.neon.tech;Database=prepround;Username=neondb_owner;Password=<…>;SSL Mode=VerifyFull;Channel Binding=Require`
     - Команди:
       ```bash
       git pull                     # імпортуємо закомічений стан
       read -rs NEON_OWNER          # вставити рядок, Enter: на екрані й в історії shell його не буде
       dotnet tool restore
       ConnectionStrings__Prepround="$NEON_OWNER" dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api
       ConnectionStrings__Prepround="$NEON_OWNER" dotnet run --project src/Prepround.ContentTool -- import ../content
       unset NEON_OWNER
       ```
     - Очікувано: обидві команди → `0`. `import` іде **без** `--include-drafts`, тож у підсумку буде `Питання: додано 564, …` або число, яке головна сесія назве після PRP-023.
  2. **Роль лише для читання** (власник).
     - Пароль генерується в терміналі: `openssl rand -hex 24`. Якщо Neon вважатиме його слабким: `openssl rand -base64 32 | tr -d '/+='`.
     - Потім у Neon SQL Editor (БД `prepround`, роль `neondb_owner`) виконати SQL з ADR 005, розділ 5: `CREATE ROLE prepround_reader …`, `GRANT …` і `ALTER DEFAULT PRIVILEGES …`.
     - API лише читає: `SaveChanges` є тільки в імпорті ContentTool. Тож зламаний API не змінить даних, а рядок власника на сервер не потрапить.
  3. **Render: Web Service** (власник). New → Web Service → репо `dmitryspain/Prepround`, гілка `master`:

     | Поле | Значення |
     |---|---|
     | Name | `prepround-api` |
     | Region | Frankfurt (EU Central) |
     | Language | Docker |
     | Root Directory | `backend` |
     | Dockerfile Path | `./Dockerfile` |
     | Docker Build Context | `.` |
     | Instance Type | Free |
     | Health Check Path | `/api/health` |

     - **Змінна середовища — одна:** `ConnectionStrings__Prepround` = `Host=<той самий direct host>;Database=prepround;Username=prepround_reader;Password=<…>;SSL Mode=VerifyFull;Channel Binding=Require;Connection Idle Lifetime=60`.
     - **Не задавати** `PORT`, `ASPNETCORE_ENVIRONMENT` і `Cors__Origins__0`: їх дають Dockerfile і `appsettings.Production.json`.
     - **Build Filters → Ignored Paths:** `backend/.claude/**`, `backend/tests/**`. Auto-Deploy — On Commit.
  4. **Cloudflare Pages** (власник). Workers & Pages → Create → Pages → Import an existing Git repository → `dmitryspain/PreproundUI`:

     | Поле | Значення |
     |---|---|
     | Project name | `prepround` (Pages запропонує `preproundui`: змінити) |
     | Production branch | `master` |
     | Framework preset | None |
     | Build command | `npm run build` |
     | Build output directory | `dist/web/browser` |
     | Root directory | порожньо |

     Змінних середовища не потрібно: Node береться з `.node-version`.
  5. **Якщо `prepround-api` чи `prepround` зайняті,** написати головній сесії фактичні адреси до кроку 6. Вона змінить рівно два рядки: `PreproundWeb/src/environments/environment.production.ts` і `Prepround/backend/src/Prepround.Api/appsettings.Production.json` (`Cors:Origins`). Власник комітить і пушить, обидва сервіси перезберуться самі.
  6. **Smoke** (головна сесія, `curl` з Мака; критерії нижче). Окремо:
     - якщо T2 не пройшов, у Render задати `ForwardedHeaders__ForwardedForHeaderName=X-Forwarded-For` і `ForwardedHeaders__ForwardLimit=2` (запасний варіант ADR 005), дочекатися деплою і повторити T1 та T2;
     - якщо не пройшов T1 (підробка обходить ліміт), зупинитись і покликати PM.
  7. **Документи** (головна сесія):
     - CLAUDE.md, розділ «Деплой» (текст запропонував PM 2026-11-19);
     - spec, розділ 6: «деплой на prepround.app» → «власний домен prepround.app»;
     - ADR 005: розділ «Факти після деплою» з фактичними адресами, версією Neon, розміром образу, часом збірки на Render і часом холодного старту (метрики для портфоліо).
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] **Neon (власник, SQL Editor):**
    - `SELECT count(*) FROM "Questions" WHERE "IsActive"` → число з кроку 1;
    - `SELECT has_table_privilege('prepround_reader', '"Questions"', 'INSERT')` → `false`, `… 'SELECT')` → `true`.
  - [ ] `curl -s --max-time 120 -w '\n%{time_total}\n' https://prepround-api.onrender.com/api/health` → `{"status":"ok"}`. Час першого запиту після простою записати
  - [ ] **каталог.** `curl -s https://prepround-api.onrender.com/api/catalog | jq '[.fields[0].topics[].questionCounts[]] | add'` → число з кроку 1. Тем 61, порядок тем збігається з локальним API
  - [ ] **CORS.** Preflight `OPTIONS /api/tests`:
    - з `Origin: https://prepround.pages.dev` → `access-control-allow-origin: https://prepround.pages.dev`;
    - з `Origin: http://localhost:4300` → заголовка немає.
  - [ ] `curl -s -o /dev/null -w '%{http_code}' https://prepround-api.onrender.com/openapi/v1.json` → `404`
  - [ ] **T1 (підробка не обходить ліміт):** 35 × `POST /api/tests` з тілом `{}` і щоразу іншими `X-Forwarded-For` та `CF-Connecting-IP` → серед перших 30 немає `429`, з 31-го — `429`
  - [ ] **T2 (ліміт ділиться за людиною, а не за проксі):** головна сесія 2 хв тримає ліміт вичерпаним (запит раз на 2 с). У цей час власник із телефона в мобільній мережі (Wi-Fi вимкнено) починає тест на `https://prepround.pages.dev` → тест стартує, без «Забагато запитів»
  - [ ] **сайт.**
    - `curl -s https://prepround.pages.dev/ | grep -c '<app-root'` → ≥ `1`;
    - `curl -s -o /dev/null -w '%{http_code}' https://prepround.pages.dev/result` → `200` з тим самим `index.html`;
    - головний JS сайту містить `prepround-api.onrender.com`.
  - [ ] **власник, телефон і десктоп:** налаштування → тест на 10 питань з «Перевірити» → результат. У DevTools → Network запити йдуть на `onrender.com`, помилок CORS немає. Після 15+ хв простою під «Завантаження…» видно підказку про пробудження сервера
  - [ ] **секретів немає в git:** `git -C /Users/dmitryspain/RiderProjects/Prepround grep -n 'neon.tech'` і `git -C /Users/dmitryspain/RiderProjects/PreproundWeb grep -n 'neon.tech'` → порожньо
  - [ ] **Render:** деплой зелений, health check проходить, у логах немає рядків із `Password=`
- **Зміна процесу після деплою** (ADR 005, розділ 7; головна сесія вносить у CLAUDE.md):
  - **Міграції.** Нову EF-міграцію власник застосовує в Neon **до** push коду, який від неї залежить.
  - **Імпорт у прод** — крок власника, без `--include-drafts`, один за раз, на зупинках хвиль і етапів. Перед імпортом — `git pull`, після нього — smoke каталогу.
  - **Агенти до Neon не ходять.** Smoke проду — лише `curl` до публічних адрес.
- **Для tester:** не стосується. Smoke робить головна сесія; якщо T1 або T2 не пройшли, вона кличе PM.
- **Контракт API:** без змін.
- **Поза скоупом:**
  - домен prepround.app, keep-alive, CI, автоімпорт, CSP («Ідеї»);
  - preview-деплої Pages: CORS їх не пропускає, і це прийнятно;
  - iOS (PRP-008).

**Примітка головної сесії (хвости PRP-021, 2026-11-19):**
- В обидва рядки підключення (власника й Render) додати `GSS Encryption Mode=Disable`. В образі немає `libgssapi_krb5.so.2`, і Npgsql у режимі `Prefer` на кожному новому з'єднанні пише в лог помилку завантаження бібліотеки. Neon GSS не використовує.
- `ForwardedHeaders__ForwardLimit` у Render задавати лише непорожнім числом або не задавати зовсім. Порожнє значення, найімовірніше, дасть `null`, тобто без ліміту.
- Архітектуру amd64 локально не перевіряли. Її перевіряє перша збірка на Render.

**Прогрес (2026-11-19):**
- **Крок 4 (Pages)** виконав власник: `https://prepround.pages.dev`.
- **Кроки 1–2** виконала головна сесія на прохання власника: він сам передав рядок власника БД («я тобі довіряю»). Фактичні назви відрізняються від картки: БД `preprounddb`, роль власника `preprounddb_owner`, Postgres 18.7.
  - `ef database update` → 0, `import ../content` → `Питання: додано 564`;
  - роль `prepround_reader` створено (SQL з ADR 005, розділ 5, з `preprounddb`): `INSERT` → `false`, `SELECT` → `true`, `DELETE` від читача → `permission denied`;
  - образ API локально проти Neon під читачем: health `ok`, каталог — 564 питання.
- Рядок для Render лежить у scratchpad у файлі з правами 600. Пароль читача в чат не потрапляв.
- Рядок власника БД побував у чаті, тож його варто змінити в Neon (Roles → Reset password).
- **Крок 3 (Render)** виконав власник: `https://prepround-api.onrender.com`. Build Filters не задано, їх можна додати пізніше.
- **Smoke API (головна сесія, 2026-11-19):**
  - health → `{"status":"ok"}` за 0,41 с (сервіс щойно задеплоєно, тож холодного старту не було);
  - каталог: 564 питання, 61 тема, порядок тем як у локального API;
  - `/openapi/v1.json` → 404;
  - CORS: `pages.dev` → `access-control-allow-origin`, `localhost:4300` → без заголовка.
- **T1 — PASS.**
  - Різні підроблені `X-Forwarded-For` → 28 × `400` (+2 запити раніше в тому самому вікні), потім `429`.
  - Підроблений клієнтом `CF-Connecting-IP` до API не доходить: Cloudflare відповідає `403`, `error code: 1000`. Отже, cloudflare сам ставить цей заголовок, і клієнт його не підмінить.
- **Секрети:** `git grep neon.tech` знаходить лише шаблони `<ep-…>` у беклозі й ADR 005. Справжнього host і паролів у репо немає.
- **Сайт — проблема.** `https://prepround.pages.dev/` і всі шляхи (`/index.html`, `/theme-init.js`) → `404` з порожнім тілом, хоча `PreproundWeb` запушено (`a8bbaa1`). Схоже, на Pages немає жодного успішного деплою. Власник дивиться Deployments і лог збірки.
- **Сайт полагоджено.** У Pages не було команди збірки й вихідної теки («No build command specified»). Власник додав обидва поля, і повторний деплій пройшов.
  - Smoke сайту: `/` містить `<app-root`, `/result`, `/settings`, `/test` → `200` з тим самим `index.html` (SPA-фолбек);
  - головний JS містить `https://prepround-api.onrender.com/api` і не містить `localhost`;
  - CORS preflight з origin сайту → `allow-origin`;
  - `POST /api/tests` (csharp + sql, 10 питань) → `200` за 1,46 с.
- **Документи (крок 7):** розділ «Деплой» у CLAUDE.md і spec, розділ 6 — внесено. Лишились факти для портфоліо в ADR 005.
- **Лишилось:** T2 (телефон у мобільній мережі), перевірка власником на телефоні й десктопі, підказка після 15+ хв простою.

### PRP-025 · Переклади в YAML: ключі `*_en`, `draft_en` і правило 11 у `validate`
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** рішення 006
- **Навіщо:** англійський текст має жити поруч з українським і з `correct` у тому самому файлі. Тоді переклад не може тихо змінити, який варіант правильний. А `validate` має зупинити неповний переклад ще до імпорту. Це перша картка етапу 5: на ній стоять БД, лінт і хвилі перекладу.
- **Що зробити** (репо `Prepround`, шляхи відносно `backend/`; формат — ADR 006, розділ 1, і ADR 002, «Уточнення: переклади»):
  1. **`src/Prepround.Application/Content/ContentLanguages.cs`** — єдиний список мов у бекенді: `Base = "uk"`, `Translations = ["en"]` і зручне `All`. Інших літералів `"uk"` / `"en"` у `src` немає.
  2. **Читач `src/Prepround.Infrastructure/Content/YamlContentReader.cs`.** Для кожної мови L з `ContentLanguages.Translations` приймає ключі:
     - `title_L` — у галузі, рівні, напрямі, стеку й темі;
     - `description_L` — у темі;
     - `draft_L` — у темі, булеве, читається так само, як `draft`;
     - `text_L` і `explanation_L` — у питанні;
     - `text_L` — у варіанті.

     Списки ключів (`FieldKeys` … `OptionKeys`) будуються з цих правил, а не вписуються вручну для `en`. Будь-який інший ключ лишається помилкою правила 9, з назвою ключа: `text_de`, `title_EN`, `source_en` чи `lang_en` у `code`.
  3. **Визначення в `src/Prepround.Application/Content/`** несуть переклади кожного поля окремо, як словник «мова → текст»: назви галузі, рівня, напряму, стеку, теми; опис теми; умову, пояснення і текст кожного варіанта. Тема несе ще множину мов, де `draft_L: true`. Окремо по полях — щоб валідатор міг назвати, чого бракує. Форма записів — на розсуд coder, без зміни наявних позиційних параметрів, від яких залежать тести (додавати в кінець або з значенням за замовчуванням).
  4. **`ContentValidator`, правило 11** (працює, як і правила 1–10, лише коли читач помилок не знайшов):
     - значення перекладу непорожнє після `Trim` — інакше помилка, як за правилом 8;
     - питання мовою L має **або всі** ключі (`text_L`, `explanation_L` і `text_L` кожного варіанта), **або жодного**. Інакше одна помилка на питання: файл, id, мова і перелік відсутніх ключів, наприклад «варіант 3: немає text_en»;
     - тексти варіантів мовою L не повторюються в межах питання (як правило 7).
  5. **`validate`, рядок підсумку галузі** (`src/Prepround.ContentTool/Program.cs`). Коли N > 0, наприкінці дописується `, перекладів en: N`, де N — питання з повним перекладом. Так само працює наявний суфікс про чернетки. Коли N = 0, рядок не змінюється.
  6. **`import`** переклади поки ігнорує (БД — PRP-026): контент із ключами `_en` імпортується успішно, і дані такі самі, як без них.
- **Дозволені пакети:** нових немає.
- **Критерії приймання** (команди з `Prepround/backend`, копії контенту — у scratchpad):
  - [ ] `dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → `0`, рядок `it: …` дослівно такий, як до картки (без «перекладів»)
  - [ ] у копії `content` у тему `csharp` додано `title_en`, `description_en`, `draft_en: true`, а в `csharp-001` — `text_en`, `explanation_en` і `text_en` кожного варіанта; у `field.yaml` — `title_en` одного рівня → `validate <копія>` → `0`, рядок закінчується на `, перекладів en: 1`
  - [ ] у цій копії прибрати `text_en` третього варіанта `csharp-001` → `1`; у stderr є рядок з `topics/csharp.yaml`, `csharp-001`, `en` і відсутнім ключем варіанта; рядка «перекладів» немає
  - [ ] окремо: `text_en: ''` (або лише пробіли) → `1`; два варіанти з однаковим `text_en` → `1`
  - [ ] окремо: `text_de: x` у питанні, `title_EN: x` у темі, `source_en: x` у `code` → кожен `1`, у повідомленні назва ключа
  - [ ] `draft_en: так` → `1` (як `draft: так`)
  - [ ] тест на Testcontainers: `import` контенту з перекладом дає ті самі рядки в БД і той самий підсумок, що й `import` того самого контенту без перекладу
  - [ ] `grep -rn '"en"' src --include='*.cs'` → лише `ContentLanguages.cs`; так само `grep -rn '"uk"' src --include='*.cs'`
  - [ ] `dotnet test; echo $?` → `0`, `failed: 0`; охоронні тести зелені: `ValidateCommandTests`, `ValidateUnreadYamlTests`, `ImportCommandTests`, `LayerDependencyTests`, `HandlerCompositionTests`
- **Для tester:**
  - помилки читача і валідатора мають той самий формат «файл: id: що не так», що й інші;
  - `draft_en` без жодного перекладу — допустимий: переклад теми тільки почався;
  - `title_en` у `field.yaml` (галузь, напрям, стек) читається;
  - правило 11 не запускається, коли читач знайшов помилки (ADR 002, «Уточнення реалізації»);
  - `code` з будь-яким ключем `_en` — помилка, а не тиша.
- **Контракт API:** без змін.
- **Поза скоупом:** БД і `import` перекладів (PRP-026), API (PRP-028), `content-lint` (PRP-029), сам переклад (PRP-030 і хвилі), правило 12 (PRP-031), переклад `code`.

### PRP-026 · БД та `import` перекладів: міграція `ContentTranslations`
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** PRP-025
- **Навіщо:** API читає лише з бази (ADR 002), тож переклади мають туди потрапити. Імпорт має довести рядком підсумку, що переклад не змінив українського змісту: у хвилях перекладу це головний механічний доказ.
- **Що зробити** (репо `Prepround`, шляхи відносно `backend/`; ADR 006, розділ 2):
  1. **Domain, назви.** У `Field`, `Level`, `Direction`, `Stack`, `Topic` додати `TitleTranslations`, а в `Topic` — ще `DescriptionTranslations`. Тип — словник «мова → текст» лише для читання. `UpdateFrom` порівнює їх за значенням, без урахування порядку ключів. Зміна перекладу назви робить сутність «оновленою».
  2. **Domain, питання.** Нова сутність `QuestionTranslation`:
     - поля: `QuestionId`, `Language`, `Text`, `OptionTexts` (тексти варіантів у порядку файлу, кількість дорівнює `Question.Options`), `Explanation`;
     - `UpdateFrom`, який каже, чи змінився вміст;
     - це **не** `CatalogEntity`, без `IsActive`: зниклий переклад видаляється (ADR 006, розділ 2).
  3. **Infrastructure:**
     - словники й `OptionTexts` — jsonb, за зразком `Configurations/QuestionConfiguration.cs` (`Options` з `ValueComparer`);
     - таблиця `QuestionTranslations`: ключ (`QuestionId`, `Language`), FK на `Questions.Id` з `Restrict`.
  4. **Міграція `ContentTranslations`** (`dotnet ef migrations add ContentTranslations …`). Вона лише додає:
     - нові jsonb-колонки `NOT NULL`, наявні рядки отримують `{}`;
     - нову таблицю.

     Старі міграції не змінюються.
  5. **`ImportContentHandler` і `ICatalogImportRepository`:**
     - **назви** переносяться з визначень (PRP-025) у сутності;
     - **бажаний набір перекладів питань** — для кожного питання, яке цей імпорт робить активним, кожна мова з повним перекладом. Мова L теми з `draft_L: true` без `--include-drafts` у набір не входить; питань тем із `draft: true` без прапорця немає, як і раніше;
     - відсутні переклади додаються, змінені оновлюються, а рядки, яких у наборі немає, видаляються. Усе в тому самому `SaveChanges` і тій самій транзакції;
     - `Question.UpdatedAt` і лічильники «Питання» від перекладів не залежать.
  6. **ContentTool:** у підсумку `import`, одразу після рядка «Питання: …», — `Переклади питань: додано N, оновлено M, видалено K`. `ImportSummary` отримує окремі лічильники.
  7. **Dev-БД:** `dotnet ef database update …`, потім `import ../content --include-drafts` через `ConnectionStrings__Prepround`.
  8. **API і його відповіді не змінюються.** Переклади читає PRP-028.
- **Дозволені пакети:** нових немає.
- **Критерії приймання** (з `Prepround/backend`):
  - [ ] `dotnet ef migrations list --project src/Prepround.Infrastructure --startup-project src/Prepround.Api` → останньою стоїть `…_ContentTranslations`; `git diff --exit-code -- src/Prepround.Infrastructure/Migrations/20261114000242_InitialCatalog.cs src/Prepround.Infrastructure/Migrations/20261114000242_InitialCatalog.Designer.cs; echo $?` → `0`
  - [ ] `dotnet ef database update …; echo $?` на dev-БД з даними → `0`. Через `docker exec prepround-postgres psql -U prepround -d prepround`:
    - `select count(*) from "Topics" where "TitleTranslations" <> '{}'::jsonb` → `0`;
    - `select count(*) from "QuestionTranslations"` → `0`
  - [ ] два `import ../content --include-drafts` поспіль → обидва `0`, у другому всі рядки, зокрема `Переклади питань`, мають нулі
  - [ ] тести на Testcontainers з фікстурою:
    - додано `en` в одне питання → `Питання: додано 0, оновлено 0, деактивовано 0` і `Переклади питань: додано 1, оновлено 0, видалено 0`;
    - змінено `explanation_en` → `оновлено 1`;
    - прибрано всі `_en` питання → `видалено 1`;
    - змінено `title_en` теми → у рядку «Теми» `оновлено 1`
  - [ ] тема з перекладом і `draft_en: true`: `import` без прапорця → її перекладів у `QuestionTranslations` немає (раніше імпортовані видалено), а питання активні; з `--include-drafts` → переклади є
  - [ ] тема з `draft: true` без прапорця → її питання деактивовано, як і раніше, а перекладів цих питань немає
  - [ ] збій посеред імпорту (наявний тест транзакції або аналог) → ні питань, ні перекладів не змінено
  - [ ] `dotnet test; echo $?` → `0`, `failed: 0`; `CatalogEndpointTests` і `TestsEndpointTests` зелені без змін очікувань (API не змінився)
- **Для tester:**
  - порівняння словників не залежить від порядку ключів;
  - jsonb `OptionTexts` — масив рядків у порядку файлу;
  - переклади деактивованого питання видаляються;
  - повторний імпорт не дає «оновлено» через іншу серіалізацію;
  - нова таблиця на іншому Postgres (Neon 18) не потребує нічого, крім міграції.
- **Для власника:** після цієї картки код `backend/` вимагає нової схеми. Тож міграцію в Neon треба застосувати **до** наступного push `backend/` (PRP-032, крок 1). Інакше `/api/catalog` на Render дасть `500`.
- **Контракт API:** без змін.
- **Поза скоупом:** читання перекладів в API (PRP-028), фронт, `content-lint`, переклад контенту.

### PRP-027 · Мова інтерфейсу: словники uk/en, перемикач і вибір мови
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** frontend
- **Платформа:** web
- **Залежить від:** рішення 006; правки spec 3.7 з ADR 006, розділ 9 (вносить головна сесія перед карткою)
- **Навіщо:** людина, яка готується до співбесіди англійською або не читає українською, має бачити зрозумілий інтерфейс і перемикати мову одним натиском, без перезавантаження. До кінця перекладу продакшн-збірка лишається однією мовою, тож push картки людям нічого не ламає.
- **Що зробити** (усе в репо `PreproundWeb`, шляхи відносно кореня, команди з нього; ADR 006, розділ 4):
  1. **`src/app/core/i18n/`.** Взірець — i18n власника в Outlay, лише читати: `/Users/dmitryspain/RiderProjects/OutlayUI-latest/src/app/i18n/`, тобто `lang.ts`, `messages/uk.ts` з `MessageKey = keyof typeof uk`, `messages/en.ts` типу `Record<MessageKey, Message>`, `translate.ts` (`translate`, `translatePlural` на `Intl.PluralRules`, `richParts` для `**жирного**` без `innerHTML`), `pipes.ts` (нечистий `t`), `errors.ts`, `i18n.service.ts`. Перенеси ту саму будову (файли `src/app/core/i18n/messages/uk.ts`, `messages/en.ts` тощо), але з відмінностями нижче.
     - `languages.ts`:
       - тип `LanguageId` = `'uk' | 'en'`;
       - токен `LANGUAGES` — доступні мови в порядку перемикача, з `environment.languages`;
       - токен `DEFAULT_LANGUAGE` — з `environment.defaultLanguage`;
       - токен `BROWSER_LANGUAGES`: фабрика бере `navigator.languages`, без нього — `[navigator.language]`, без обох — `[]`. Це шов для тестів, без прямих звернень до `navigator` деінде;
       - власні назви мов (`Українська`, `English`), однакові в обох словниках.
     - `messages/uk.ts` (джерело ключів) і `messages/en.ts` — **усі** тексти інтерфейсу. Відсутній або зайвий ключ у `en` ламає збірку, як в Outlay. Параметри — `{name}`, множина — `translatePlural`. Англійська природна, а не калька: «Distributed X of Y», «Check», «Finish test», «Only mistakes».
     - Сигнал мови (як `currentLang` в Outlay) виставляє `PrefsService`. Шаблони читають тексти через нечистий pipe `t`, код — через `translate()`. Сервіс `I18n` дає `language` (лише читання), `languages` і `setLanguage`.
  2. **`src/environments/`:**
     - `environment.ts`: `languages: ['uk', 'en']`, `defaultLanguage: 'en'`;
     - `environment.production.ts`: `languages: ['uk']`, `defaultLanguage: 'uk'`. Англійську в проді вмикає PRP-031.
  3. **`core/prefs.service.ts`** лишається єдиним, хто пише `prepround.prefs.v1`:
     - необов'язкове поле `language`;
     - сигнал `language`, у якому вже вибрана мова:
       1. збережене значення, якщо воно є в `LANGUAGES`;
       2. інакше перша мова з `BROWSER_LANGUAGES`, чий основний підтег є в `LANGUAGES` (`uk-UA` → `uk`, `EN-gb` → `en`);
       3. інакше `DEFAULT_LANGUAGE`;
     - `setLanguage(id)`: зберігає лише після дії людини, як `save()`, з тими самими `theme` і `accent`. Мову не з `LANGUAGES` ігнорує;
     - `document.documentElement.lang` = вибрана мова, одразу на старті й після кожної зміни (поруч із `data-theme` в `applyDom`).

     Ключ лишається `v1`: запис без `language` валідний. `public/theme-init.js` і `src/index.html` не змінюються.
  4. **Перемикач `src/app/shared/language-switch/`:**
     - у шапці (`app.html`) — поруч із «Налаштування», у тій самій гілці `@else`, тож під час тесту його немає. Це кнопка з назвою **іншої** мови і з атрибутом `lang` цієї мови. Позначок «UK» і «UA» немає;
     - на `/settings` — розділ «Мова» з усіма мовами (`role="radio"` / `aria-checked`, як у виборі теми);
     - коли в `LANGUAGES` одна мова, перемикача немає ні в шапці, ні на `/settings`;
     - без View Transitions.
  5. **Перенести в словники всі тексти інтерфейсу:**
     - `app.html`, `setup/setup.html`, `test-run/test-run.html`, `test-run/test-run.ts` (`stateLabels`), `result/result.html`, `result/result-view.html`, `settings/settings.html`;
     - `core/themes.ts`: описи тем, назва й опис «Авто», назви акцентів. Акцент отримує стабільний `id` замість українського `name`. Назви Aurora, Daylight, Terminal однакові в обох мовах;
     - `core/api.service.ts`: тексти помилок (п. 6).

     Коментарі, що цитують українські тексти, переписати англійською з назвою ключа.
  6. **Помилки API за кодом.** `ApiError.message` береться зі словника поточної мови в момент помилки, а `message` з тіла відповіді інтерфейс більше не показує:
     - `400`, `code` ∈ {`Test.FieldNotFound`, `Test.UnknownLevel`, `Test.UnknownTopic`, `Test.NotEnoughQuestions`} → `errors.catalogChanged` («Каталог оновився. Оновіть сторінку й перевірте розподіл.»);
     - будь-який інший `400` (з кодом чи без, зокрема ProblemDetails) → `errors.rejected` («Сервер відхилив налаштування тесту.»);
     - `429` → `errors.tooManyRequests`;
     - status `0` → `errors.network`;
     - решта → `errors.server`;
     - `errorText()` для не-`ApiError` → `errors.server`.
  7. **`PreproundWeb/CLAUDE.md`:** правило «тексти інтерфейсу лише в `src/app/core/i18n/messages/`; у компонентах і сервісах кирилиці немає; мову читаємо через `I18n`».
- **Дозволені пакети:** нових немає. `@angular/localize` не додавати.
- **Критерії приймання** (з кореня `PreproundWeb`):
  - [ ] `grep -rlE '[А-ЯҐЄІЇа-яґєії]' src/app --include='*.ts' --include='*.html' | grep -v -e '/i18n/' -e '\.spec\.ts$'` → порожньо
  - [ ] прибрати один ключ з `messages/en.ts` або додати зайвий → `npm run build` → код ≠ 0 (tester перевіряє і повертає файл)
  - [ ] юніт-тести вибору мови з фейками `STORAGE` і `BROWSER_LANGUAGES`, без збереженого вибору:
    - `['uk-UA', 'en']` → `uk`;
    - `['en-GB']` → `en`;
    - `['de-DE']` → `DEFAULT_LANGUAGE`;
    - `[]` → `DEFAULT_LANGUAGE`
  - [ ] те саме зі збереженим вибором:
    - збережене `en` при браузері `['uk']` → `en`;
    - збережене `de` → як без збереженого;
    - `LANGUAGES = ['uk']` і збережене `en` → `uk`
  - [ ] `setLanguage('en')` → у `prepround.prefs.v1` `language: 'en'`, `theme` і `accent` ті самі; `document.documentElement.lang` → `en`. `setLanguage('de')` → сховище й `lang` без змін
  - [ ] старий запис `{"theme":"terminal","accent":120}` без `language` → тема й акцент застосовані, мова — за браузером
  - [ ] компонентні тести:
    - з мовою `en` і фейковим каталогом з латинськими назвами `textContent` сторінок `/`, `/settings`, а також `/test` і `/result` з фейковим тестом латиницею не містить кирилиці;
    - натиск перемикача в шапці змінює мову без навігації, і тексти `/` стають англійськими;
    - на `/test` перемикача немає;
    - з `LANGUAGES = ['uk']` його немає ні в шапці, ні на `/settings`
  - [ ] маппінг помилок через `HttpTestingController` (правило 25), мовами `uk` і `en`:
    - `400 {code:'Test.NotEnoughQuestions', message:'У темі…'}` → `catalogChanged`;
    - `400 {code:'Test.CountTooSmall', message:'…'}` → `rejected`;
    - `400` ProblemDetails без `message` → `rejected`;
    - `429`, `500`, status `0` → свої тексти;
    - у жодному результаті немає тексту з `message` тіла
  - [ ] `npm run build; echo $?` → `0` без попереджень; `npx ng test --watch=false; echo $?` → `0`
- **Для tester:**
  - **Наявні специфікації** перевіряють українські тексти, а jsdom дає `navigator.languages = ['en-US']`. Тож у них можна закріпити `uk` (провайдер `BROWSER_LANGUAGES` або збережений вибір) і перевіряти той самий факт (правило 28).
  - **Якість англійської:** природні формулювання, однакові терміни (question, topic, level, test), числа без зламаних відмінків.
  - **Скріншоти** `/`, `/test`, `/result`, `/settings` англійською на 390 px — у звіт. Шапка з новою кнопкою не дає горизонтальної прокрутки на 390 px.
  - **`lang`** кнопки перемикача відповідає мові її назви.
  - **Збірки:** у dev-збірці перемикач є, а `npm run build` (production) збирається з однією мовою.
- **Контракт API:** без змін. Мову контенту передає PRP-028.
- **Поза скоупом:**
  - `?lang=` у запитах і перезавантаження каталогу (PRP-028);
  - переклад контенту;
  - `theme-init.js`;
  - формат дат (примітка в PRP-012);
  - `<title>` / `meta` мовою (у «Ідеях»);
  - iOS (PRP-008).

### PRP-028 · Мова контенту в API: `?lang=` у каталозі й тестах, фронт шле мову
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend + frontend
- **Платформа:** web
- **Залежить від:** PRP-026, PRP-027; spec 5, 5.1 і 5.2 оновлено за ADR 006, розділ 9
- **Навіщо:** без цього англійський інтерфейс показує українські назви й питання. Англійський тест бере лише перекладені питання, тож людина ніколи не отримає питання, якого не може прочитати. Без параметра API відповідає як зараз, тож старий фронт і push у будь-якому порядку нічого не ламають.
- **Що зробити** (ADR 006, розділи 3–4; шляхи з префіксом репо):
  1. **`Prepround/backend/src/Prepround.Api/Controllers/CatalogController.cs` і `TestsController.cs`:** параметр запиту `lang` (`[FromQuery(Name = "lang")]`), тіло `POST /api/tests` без змін. Мова передається в `GetCatalogQuery` / у хендлер тестів.
  2. **Перевірка мови** — одна для обох ендпоінтів, у Application:
     - відсутній або порожній `lang` → `ContentLanguages.Base`;
     - значення не з `ContentLanguages.All` (точний збіг, регістр важливий) → `400` `{ "code": "Language.Unsupported", "message": "Мову «<значення>» не підтримано. Доступні: uk, en." }`;
     - у тестах ця перевірка йде першою, до всіх правил spec 5.2.
  3. **`GetCatalogHandler`:**
     - назви галузі, рівнів, напрямів, стеків, тем і описи тем — мовою запиту; без перекладу — базова;
     - теми сортуються за **показаною** назвою тим самим компаратором (`InvariantCulture`, нічия → id);
     - `questionCounts` рахує активні питання з перекладом мовою запиту, а для бази — як зараз. Тема без таких питань → `{}`.
  4. **`GenerateTestHandler` і `TestGenerationRepository`:**
     - кандидати — лише питання з перекладом мовою запиту (для бази — як зараз). Від них рахуються ліміт і `Test.NotEnoughQuestions`;
     - у відповіді `text`, `options[].text` (за `key` = індекс у файлі) і `explanation` — з перекладу; `id`, `topicId`, `levelId`, `type`, `code`, `key`, `isCorrect` — з базового рядка;
     - переклади читаються лише для вибраних питань, без завантаження всіх.
  5. **Відповідь без `lang`** побайтово така сама, як із `lang=uk`, і як до картки.
  6. **`PreproundWeb/src/app/core/api.service.ts`:** `getCatalog()` і `createTest()` самі додають `lang=<I18n.language()>` до кожного запиту (`HttpParams`). Сигнатури для викликачів і моделі не змінюються.
  7. **`PreproundWeb/src/app/setup/setup.ts`:** після зміни мови каталог перезавантажується цією мовою.
     - **Зберігаються** вибрані галузь, напрям, стек, рівні, рядки тем (за id), їхні кількості, `total` і таймер.
     - **Кількість понад новий максимум** знижується до максимуму (spec 3.1, п. 5). Тема без питань цією мовою стає неактивною з «0 питань».
     - **Діє відповідь на останню вибрану мову:** uk → en → uk підряд закінчується українським каталогом.
     - **Невдале перезавантаження** дає той самий стан помилки, що й невдале перше завантаження.
  8. **`/test` і `/result` нічого не перезавантажують.** Тест і результат лишаються мовою, якою тест почато (`TestSession.field`). «Пройти ще раз» іде через `start`, тобто поточною мовою.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] `curl`, з `Prepround/backend`, на API, перезапущеному головною сесією з кодом картки:
    - `curl -sk https://localhost:7120/api/catalog | shasum` дорівнює `…/api/catalog?lang=uk` і `…?lang=` та знімку, який coder зберіг до змін;
    - `…/api/catalog?lang=de`, `?lang=EN` → `400`, `code` = `Language.Unsupported`;
    - `curl -sk -X POST 'https://localhost:7120/api/tests?lang=de' -H 'Content-Type: application/json' -d '{}'` → `400` `Language.Unsupported`
  - [ ] інтеграційні тести (Testcontainers, фікстура: тема A — 12 питань, з них перекладено 10, і `title_en`; тема B — 12 питань без перекладів і без `title_en`):
    - `?lang=en` → назва й опис A англійською, назва B українською;
    - сума `questionCounts` A — `10`, у B — `{}`;
    - порядок тем — за англійськими назвами
  - [ ] `POST /api/tests?lang=en` з A `count` 10 → `200`, 10 питань з англійськими `text`, `options[].text` і `explanation`; `key` і `isCorrect` ті самі, що в `uk` для тих самих id. З A `count` 11 → `400` `Test.NotEnoughQuestions`. З `lang=uk` і A `count` 12 → `200`
  - [ ] `HttpTestingController` (`PreproundWeb`): при мові `en` URL `getCatalog()` і `createTest()` містить `lang=en`, тіло `createTest` без змін; при `uk` — `lang=uk`
  - [ ] тест Setup з фейковим `ApiService`, що має два каталоги:
    - стек, рівні й кількості вибрано → після перемикання мови ті самі id і кількості;
    - кількість понад новий максимум знижено, тема з 0 неактивна;
    - uk → en → uk з відповідями у зворотному порядку → український каталог
  - [ ] `dotnet test; echo $?` → `0` (`Prepround/backend`), `npx ng test --watch=false; echo $?` → `0` і `npm run build; echo $?` → `0` (`PreproundWeb`)
  - [ ] охоронні тести зелені: `CatalogEndpointTests`, `TestsEndpointTests`, `CorsTests`, `HandlerCompositionTests`, `LayerDependencyTests`
- **Для tester:**
  - жодних N+1 у генерації тесту (лог SQL або підрахунок запитів);
  - назва без перекладу береться базовою по кожному полю окремо;
  - деактивовані питання з перекладом не потрапляють у лічильники й тест;
  - `lang=%20en` дає `400`;
  - rate limit і CORS-preflight від параметра не залежать;
  - spec 5 збігається з поведінкою.
- **Контракт API:**
  - spec 5, вступ: параметр `lang`, `Language.Unsupported`, `message` для розробника;
  - spec 5.1: назви мовою, сортування, `questionCounts`;
  - spec 5.2: правило «Мова».

  Форма DTO не змінюється. За правилом 003 картка оновлює обидва репо.
- **Поза скоупом:** переклад контенту, `content-lint`, правило 12 і `languages` у проді (PRP-031), переклад повідомлень помилок на сервері (ADR 006, «Відкинуто»).

### PRP-029 · `content-lint` для англійської: довжини, кирилиця, inline-код, `--base` і `--translation-only`
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend
- **Залежить від:** PRP-025
- **Навіщо:**
  - **Перевага довжини підказує відповідь будь-якою мовою.** Те, що лінт ловить в українській, має ловити й в англійській.
  - **Хвилям перекладу потрібен механічний доказ**, що змінились лише англійські ключі. Тоді tester читає зміст, а не шукає зміни очима.
  - **Правка опублікованого питання не має лишити англійську застарілою.**
- **Що зробити** (`Prepround/backend/.claude/tools/content-lint.py`; ADR 006, розділ 5; мову перекладу задати однією константою, як у `ContentLanguages`):
  1. **Довжини англійською.** Для питань теми з `_en` ті самі правила `lengths` рахуються по `text_en` варіантів: частки «найдовша» по `single` з перекладом і `MAX_RATIO`. Рядок помилки позначає мову, наприклад `[en] csharp-004: правильна …`.
  2. **`FORBIDDEN_OPTION`** застосовується і до `text_en` варіантів.
  3. **Повнота теми.** Якщо хоча б одне питання теми має переклад, а не всі, — `✗ [en] переклад неповний: N з M питань`.
  4. **Кирилиця** в `text_en`, `explanation_en`, `text_en` варіантів, `title_en`, `description_en` поза бектиками → `✗ [en] <id>: кирилиця в <поле>`.
  5. **Inline-код.**
     - У варіанті мультимножина фрагментів `` `…` `` у `text_en` має дорівнювати мультимножині в `text` → інакше `✗`.
     - В умові й поясненні порівнюються множини; розбіжність → `!` (попередження, код виходу не змінює).
  6. **`--base <тека>`** — корінь копії контенту. Кожна тема порівнюється з файлом за тим самим відносним шляхом `<галузь>/topics/<тема>.yaml`; якщо файлу в базі немає, тема пропускається з рядком `!`.
     - **Застарілий переклад:** базове поле (`text`, `explanation`, `text` варіанта, `title`, `description`) змінилось, у базі для нього був `_en`, а сам `_en` не змінився → `✗ <id>: змінено uk без en (<поле>)`.
  7. **`--translation-only`** (лише разом з `--base`). Будь-яка відмінність від бази, крім ключів `*_en` і `draft_en`, дає `✗ <id>: змінено не лише переклад (<поле>)`. Це стосується нових, зниклих чи переставлених питань і варіантів, `level`, `type`, `correct`, `code`, `sources`, базових текстів, `draft`.
  8. **Підсумковий рядок теми** дописує ` · en N/M`. `--base` без теки, з неіснуючою текою або `--translation-only` без `--base` → exit `2` з повідомленням.
  9. **Docstring** описує нові перевірки й прапорці. Без перекладів (як увесь контент зараз) результат той самий, що до картки.
- **Дозволені пакети:** нових немає (лише стандартна бібліотека і PyYAML, як зараз).
- **Критерії приймання** (з `Prepround/backend`; фікстура — копія `content/` у scratchpad; англійський текст для фікстури можна згенерувати скриптом, замінивши кожну кириличну літеру на `x`: довжини й бектики збережуться):
  - [ ] `python3 .claude/tools/content-lint.py --all; echo $?` → `0`
  - [ ] фікстура з повністю «перекладеною» темою `csharp` (`text_en`, `explanation_en`, `text_en` варіантів) → `0`, у підсумку `en 12/12`
  - [ ] у фікстурі по одній зміні, кожна → `1` і рядок `✗` з id:
    - кирилиця поза бектиками в `text_en` варіанта;
    - прибраний `` `…` `` з `text_en` варіанта;
    - правильний `text_en` у `single` у 1,5 раза довший за найдовший дистрактор;
    - `text_en` варіанта — «All of the above»;
    - прибрано переклад одного питання (`en 11/12`)
  - [ ] прибраний фрагмент `` `…` `` з `explanation_en` → `0` і рядок `!`
  - [ ] `--base <фікстура до перекладу> --translation-only` для перекладеної теми → `0`. Після зміни `level`, `correct`, `code.source` чи українського `text` будь-якого питання → `1` з id і назвою поля
  - [ ] база з перекладом; змінено лише українське `explanation` питання → `--base <база>` → `1` («змінено uk без en»). Змінено обидва → `0`
  - [ ] `--base /немає/такої/теки` → `2`; `--translation-only` без `--base` → `2`
  - [ ] `--urls` працює, як раніше (одна тема з `--urls` → `0`)
- **Для tester:**
  - фікстури — лише в scratchpad, `content/` не змінюється (`git status --short ../content` → порожньо);
  - YAML, що не парситься, поводиться як раніше;
  - бектики в лапках YAML (`'`…`'`) розбираються правильно;
  - повідомлення українською, у тому самому стилі.
- **Контракт API:** без змін.
- **Поза скоупом:** `validate` (C#, PRP-025 і PRP-031), переклад контенту, перевірка змісту перекладу (робить tester).

### PRP-030 · Назви тем англійською й англійські коментарі в коді
- **Статус:** todo
- **Виконавець:** content-author
- **Пріоритет:** P1
- **Область:** content
- **Залежить від:** PRP-025, PRP-029
- **Навіщо:**
  - **Каталог англійською.** Англійський каталог показує назви й описи тем, за якими людина вибирає, що тренувати. Без них англійський `/` залишиться українським.
  - **Коментарі в коді.** Код спільний для обох мов (ADR 006, розділ 1), тож кириличні коментарі незрозумілі англійському читачеві. Перекласти їх треба до хвиль, щоб хвилі стали «лише додаванням» і `--translation-only` проходив.
- **Що зробити** (репо `Prepround`; формат — `translation-brief.md`, «Як перекладати»):
  1. **Усі 61 файл `content/it/topics/*.yaml`:**
     - `title_en` одразу після `title`;
     - `description_en` одразу після `description`, якщо він є. Описи — переклад колонки «Покриває» spec 4.2 у тій самій формі (через кому, з крапкою, як у `description`);
     - назви, що вже англійські (`C#`, `Entity Framework Core`, `Spring Boot`), повторюються дослівно;
     - терміни — як в офіційній документації: «SQL і реляційні БД» → «SQL and relational databases».
  2. **Коментарі в коді.** У `code.source` перекласти англійською **лише коментарі** (`//`, `#`, `--`, `/* */`, `<!-- -->`) таких питань: `angular-components-002`, `api-design-009`, `django-006`, `django-010`, `go-db-005`, `go-db-008`, `hibernate-jpa-002`, `hibernate-jpa-005`, `hibernate-jpa-012`, `java-core-009`, `nestjs-012`, `nosql-007`, `spring-boot-004`, `spring-boot-006`, `symfony-004`, `vue-composition-api-009`, `vue-core-009`, `vue-core-011`.
     - Рядкові літерали й тексти в розмітці (`'привіт'`, `Зберегти`, `"немає"`) **не чіпати**: від них залежать вивід і варіанти.
     - Якщо коментар у варіанті чи поясненні цитується дослівно, оновити й цю цитату. Це єдиний дозволений виняток, і його треба назвати у звіті.
  3. **Більше нічого не змінювати.** `draft`, теми-чернетки й питання поза списком не чіпати. `field.yaml` лишається без змін: його назви вже латинські.
- **Дозволені пакети:** —
- **Критерії приймання** (з `Prepround/backend`; головна сесія перед карткою знімає `cp -R ../content <scratchpad>/before-030`):
  - [ ] `dotnet run --no-build --project src/Prepround.ContentTool -- validate ../content; echo $?` → `0`
  - [ ] команда `python3 -c "import yaml,glob; fs=sorted(glob.glob('../content/it/topics/*.yaml')); bad=[f for f in fs if not (d:=yaml.safe_load(open(f))).get('title_en') or (d.get('description') and not d.get('description_en'))]; print(len(fs), bad)"` → `61 []`
  - [ ] `python3 .claude/tools/content-lint.py --all --base <scratchpad>/before-030; echo $?` → `0`. Кирилиці в `title_en` / `description_en` немає
  - [ ] `content-lint --all --base <scratchpad>/before-030 --translation-only` → рядки `✗` є лише вигляду «`<id>`: змінено не лише переклад (`code`…)» і лише для id зі списку п. 2 (і цитат, названих у звіті)
  - [ ] `diff -r <scratchpad>/before-030/it/field.yaml ../content/it/field.yaml` → порожньо
  - [ ] у змінених `code.source` змінились лише рядки-коментарі (tester читає `diff`), рядкові літерали ті самі
  - [ ] на знімку `import --include-drafts` → `Теми: додано 0, оновлено 61, деактивовано 0`, `Питання: додано 0, оновлено <кількість питань зі зміненим code>, деактивовано 0`, `Переклади питань: додано 0, оновлено 0, видалено 0`
- **Для tester:**
  - **Назви й описи:** 100 % — зміст, терміни, природність, однаковість, назви продуктів без перекладу.
  - **Коментарі:** зміст той самий, мова коду не зламана (коментар не з'їв код). Для питань, де відповідь залежить від виводу, запуск не потрібен лише тоді, коли змінились тільки рядки-коментарі.
- **Контракт API:** без змін.
- **Поза скоупом:** переклад питань (хвилі перекладу), коментарі в темах-чернетках (`angular-di-002`, `pinia-002`, `pinia-005`, `pinia-011`, `vue-router-008` — їх правлять автори, коли відновляться хвилі Frontend), `field.yaml`.

### PRP-031 · Англійська в проді: правило 12 і `languages` у продакшн-збірці
- **Статус:** todo
- **Виконавець:** coder
- **Пріоритет:** P1
- **Область:** backend + frontend
- **Платформа:** web
- **Залежить від:** PRP-028, PRP-030; хвилі перекладу завершено: кожна з 47 тем має PASS або лишилась із `draft_en: true`
- **Навіщо:**
  - **Правило 12.** Після хвиль опублікована тема не може втратити англійську непомітно: нове питання без перекладу не пройде `validate`, коли тема стане опублікованою.
  - **Англійська в прод-збірці.** Перемикач з'являється людям лише тоді, коли англійський контент повний.
- **Що зробити:**
  1. **`Prepround/backend/src/Prepround.Application/Content/ContentValidator.cs`, правило 12** (ADR 002, «Уточнення: переклади»). Для кожної мови L з `ContentLanguages.Translations` тема без `draft: true` і без `draft_L: true` має:
     - `title_L`;
     - `description_L`, якщо `description` непорожній;
     - повний переклад мовою L кожного питання.

     Помилка: файл, id теми або питання, мова. Порожня тема-чернетка правила не порушує.
  2. **`PreproundWeb/src/environments/environment.production.ts`:** `languages: ['uk', 'en']`, `defaultLanguage: 'en'`.
- **Дозволені пакети:** нових немає.
- **Критерії приймання:**
  - [ ] `dotnet run --project src/Prepround.ContentTool -- validate ../content; echo $?` → `0` (з `Prepround/backend`)
  - [ ] у копії контенту в опублікованому питанні прибрати всі `_en` → `1`, рядок з id і `en`. Та сама копія з `draft_en: true` у темі → `0`; з `draft: true` → `0`
  - [ ] прибрати `title_en` опублікованої теми → `1`; прибрати `description_en` → `1`
  - [ ] `grep -nE "languages: \['uk', 'en'\]" src/environments/environment.production.ts` → 1 рядок; `npm run build; echo $?` → `0` (з `PreproundWeb`)
  - [ ] `dotnet test; echo $?` → `0`, `npx ng test --watch=false; echo $?` → `0`
- **Для tester:**
  - правило 12 не запускається, коли читач знайшов помилки;
  - теми з `draft_en` після хвиль перелічено в беклозі;
  - `ImportCommandTests` і фікстури тестів, що мають опубліковані теми без перекладу, оновлено свідомо (фікстура з `draft_en` або з перекладом).
- **Для власника:** push `PreproundWeb` з цією карткою — лише після прод-імпорту перекладу (PRP-032, крок 3). Інакше англійська з'явиться без питань.
- **Контракт API:** без змін.
- **Поза скоупом:** переклад тем, що лишились із `draft_en`; SEO («Ідеї»); третя мова.

### PRP-032 · Деплой англійської версії: міграція, прод-імпорт, smoke
- **Статус:** todo
- **Виконавець:** власник (кроки 1, 3, 5), головна сесія (кроки 2, 4, 6)
- **Пріоритет:** P1
- **Область:** backend + frontend
- **Платформа:** web
- **Залежить від:** PRP-026 (крок 1), PRP-025…PRP-029 (крок 2), PRP-031 і хвилі перекладу (кроки 3–6)
- **Навіщо:** англійська версія доходить до людей і не ламає української. Кроки впорядковано так, що сайт ніколи не показує англійської без питань.
- **Що зробити** (CLAUDE.md, «Деплой»; рядок підключення лише через `read -rs`):
  1. **Міграція в Neon** (власник; після PRP-026 і **до** наступного push `backend/`), з `Prepround/backend`:
     ```bash
     git pull
     read -rs NEON_OWNER      # рядок ролі preprounddb_owner, direct endpoint
     dotnet tool restore
     ConnectionStrings__Prepround="$NEON_OWNER" dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api
     unset NEON_OWNER
     ```
     Потім у Neon SQL Editor (БД `preprounddb`):
     ```sql
     SELECT has_table_privilege('prepround_reader', 'public."QuestionTranslations"', 'SELECT');
     ```
     Має бути `t`. Якщо `f`, виконати `GRANT SELECT ON public."QuestionTranslations" TO prepround_reader;`: значить `ALTER DEFAULT PRIVILEGES` виконано від іншої ролі (ADR 005, розділ 5).
  2. **Push карток PRP-025…PRP-029** в обидва репо, у будь-якому порядку після кроку 1. Smoke (головна сесія, `curl` до публічних адрес):
     - `/api/catalog` без `lang` і з `lang=uk` дають однаковий `shasum`, 564 питання;
     - `?lang=de` → `400` `Language.Unsupported`;
     - `?lang=en` → `200`;
     - на сайті перемикача немає: у проді одна мова.
  3. **Прод-імпорт перекладу** (власник; після хвиль і PRP-031, усе закомічено й запушено):
     ```bash
     git pull
     read -rs NEON_OWNER
     ConnectionStrings__Prepround="$NEON_OWNER" dotnet run --project src/Prepround.ContentTool -- import ../content
     unset NEON_OWNER
     ```
     Імпорт іде без `--include-drafts`, і очікувано:
     - `Теми: … оновлено 61`;
     - `Питання: … оновлено <кількість із PRP-030>`;
     - `Переклади питань: додано N` (N — питання тем без `draft_en`; головна сесія називає число перед кроком).
  4. **Smoke після імпорту** (головна сесія):
     - `curl -s 'https://prepround-api.onrender.com/api/catalog?lang=en' | jq '[.fields[].topics[].questionCounts[]] | add'` → N;
     - те саме без `lang` → 564, тобто не змінилось;
     - назви тем `?lang=en` без кирилиці: `jq -r '.fields[].topics[].title'` + `grep -c '[А-Яа-яІіЇїЄєҐґ]'` → `0`;
     - `POST /api/tests?lang=en` на 10 питань однієї теми → `200`, тексти англійською.
  5. **Push `PreproundWeb` з PRP-031** (власник) — лише після кроку 4 з N > 0. Потім відкрити `https://prepround.pages.dev`:
     - у шапці є «English»;
     - після перемикання тексти й теми англійською;
     - пройти тест на 10 питань англійською з телефона й десктопа;
     - повернутись на «Українська» — усе як було.
  6. **Документи** (головна сесія): CLAUDE.md, «Деплой» — рядок «англійська в проді з <дата>, N перекладених питань»; беклог — `done`, хвости.
- **Дозволені пакети:** —
- **Критерії приймання:**
  - [ ] крок 1: `dotnet ef database update` → `0`, `has_table_privilege` → `t`
  - [ ] крок 2: `shasum` каталогу без `lang` і з `lang=uk` однаковий; `?lang=de` → `400`
  - [ ] крок 4: сума `questionCounts` англійською дорівнює N з рядка імпорту, українською — 564; кирилиці в англійських назвах тем `0`
  - [ ] крок 5: власник пройшов тест англійською на телефоні й десктопі
- **Для tester:** не залучається. Smoke — лише `curl` до публічних адрес; агенти до Neon не ходять.
- **Контракт API:** без змін (spec 5 оновлено в PRP-028).
- **Поза скоупом:** власний домен, SEO, автоімпорт («Ідеї»).

---

## Відкладено: iOS

> Рішення власника 2026-11-14: iOS-застосунок відкладено повністю, без дати. ADR 001 не змінюється (Capacitor лишається в стеку на майбутнє), змінено лише пріоритет. Оболонка житиме в репо `PreproundWeb` (рішення 003). Сюди зібрано все, що вирізано з PRP-002, PRP-007 і PRP-009–PRP-011, а також колишній етап «iPhone власника».

### PRP-008 · iOS: збірка `mobile`, оболонка Capacitor і перевірка екранів на симуляторі
- **Статус:** deferred (власник, 2026-11-14)
- **Виконавець:** coder (пункт Г — власник)
- **Пріоритет:** — (після повернення до iOS — P1)
- **Область:** frontend + backend (CORS). Пункти А–В і Д — у репо `PreproundWeb` (рішення 003: оболонка в `PreproundWeb/ios/`), CORS — у `Prepround/backend`
- **Платформа:** ios
- **Залежить від:** рішення власника повернутися до iOS; PRP-012 (екрани веб-MVP готові)
- **Навіщо:** той самий застосунок на iPhone власника.
- **Перед поверненням (PM):**
  - розбити на 2–3 картки за пунктами А–В (одна картка — один підхід coder);
  - наживо перевірити версії Capacitor (8.6.2 на 2026-11-13), Xcode, runtime симуляторів і чи досі `cap run` ламається через DeviceHub (правило 5);
  - запропонувати повернути iOS-розділи в CLAUDE.md, `coder.md` і `tester.md` (шляхи iOS там тепер у `PreproundWeb`, а не `frontend/`);
  - картка буде міжрепозиторною (CORS у `Prepround`), тож шляхи з префіксом репо (рішення 003, п. 5).
- **Що зробити:**
  - **А · Збірка `mobile` і оболонка** (урок Outlay: окрема тека з першого дня, правило 14):
    - `angular.json`: конфігурація `mobile` з `fileReplacements` на `src/environments/environment.mobile.ts` і `outputPath` → `dist/mobile`; скрипт `build:mobile`;
    - `environment.mobile.ts`: `apiBaseUrl` (симулятор — `https://localhost:7120/api`, iPhone — адреса Tailscale Serve з пункту Г) і маркер `platform: 'mobile'`; в `environment.ts` — `platform: 'web'`;
    - Capacitor: `@capacitor/core`, `@capacitor/cli`, `@capacitor/ios`; `npx cap init Prepround io.github.dmitryspain.prepround --web-dir dist/mobile/browser`, конфіг у `capacitor.config.ts`; `npx cap add ios` (SPM); `TARGETED_DEVICE_FAMILY = 1`, мінімум iOS 17.0;
    - скрипти `ios:sync` = `npm run build:mobile && npx cap sync ios`, `ios:open` = `npx cap open ios`;
    - `.gitignore`: `ios/App/App/public` і `ios/App/App/capacitor.config.json`;
    - бекенд: `capacitor://localhost` у `Cors:Origins` (`appsettings.json`), бо WebView шле запити з цього origin.
  - **Б · Платформа й safe-area:**
    - `core/platform.ts`: токен `IS_NATIVE_APP` (веб — `false`, native — `Capacitor.isNativePlatform()`). На native ставиться `html[data-native]`. Платформні відмінності — лише через них;
    - `index.html`: `viewport-fit=cover`; оболонка застосунку враховує `env(safe-area-inset-*)`;
    - маркер на старті в консоль: `[Prepround] API: ok` або `[Prepround] API: недоступний` (Capacitor пересилає консоль WebView у нативний лог);
    - README «iOS»: `ios:sync`, збірка під симулятор, запуск через `xcrun simctl`, dev-сертифікат у симулятор (`dotnet dev-certs https --export-path <file>.pem --format PEM`, потім `xcrun simctl keychain <id> add-root-cert <file>.pem`). Після кожного поновлення сертифіката це треба повторити.
  - **В · iOS-перевірка готових екранів:** налаштування (PRP-009), проходження (PRP-010), результат (PRP-011), історія (PRP-012).
  - **Г · iPhone власника** (колишній етап 4, виконавець — власник): Tailscale Serve, Personal Team (профіль 7 днів, до 3 застосунків на пристрій, одне місце займає Outlay; APNs, TestFlight і App Store недоступні), адреса API в `environment.mobile.ts`.
  - **Д · Ідея:** сховище через `@capacitor/preferences` замість `localStorage`. Шов `STORAGE` з PRP-007 дозволяє замінити реалізацію, не чіпаючи решту коду.
- **Дозволені пакети:** `@capacitor/core`, `@capacitor/cli`, `@capacitor/ios` 8.6.2 (станом на 2026-11-13; при поверненні перевірити).
- **Критерії приймання** (зібрані з вирізаного, уточнити при розбитті):
  - [ ] `npm run build:mobile; echo $?` → `0`, є `dist/mobile/browser/index.html`. У `dist/web` немає маркера `platform: 'mobile'`, а в `dist/mobile` — маркера `'web'`
  - [ ] `npm run ios:sync; echo $?` → `0`; `ios/App/App/public/index.html` зібраний конфігурацією `mobile`
  - [ ] `xcodebuild … CODE_SIGNING_ALLOWED=NO build` (команда з CLAUDE.md) → `BUILD SUCCEEDED`, код `0`
  - [ ] iPhone 17 з iOS 27.0: з запущеним API й доданим сертифікатом `xcrun simctl launch --console-pty <id> io.github.dmitryspain.prepround` показує `[Prepround] API: ok`. Те саме на iPhone 15 з iOS 17.5 (мінімальна версія)
  - [ ] `curl -sk -i -X OPTIONS https://localhost:7120/api/health -H "Origin: capacitor://localhost" -H "Access-Control-Request-Method: GET"` → `Access-Control-Allow-Origin: capacitor://localhost`; `https://evil.example` як і раніше без заголовка
  - [ ] з кореня `PreproundWeb`: `git check-ignore ios/App/App/public` → шлях виводиться
  - [ ] `IS_NATIVE_APP` на вебі `false`, `html[data-native]` на вебі відсутній (тест із фейком)
  - [ ] на симуляторі пройдено тест із 10 питань (seed): налаштування → проходження з «Перевірити» → результат → історія. Скріншоти у світлій і темній темі системи (`xcrun simctl ui booted appearance light|dark`)
  - [ ] варіанти відповідей мають висоту ≥ 44 pt (скріншот)
  - [ ] `npm run build` (веб) не зламано, `npx ng test --watch=false` → `0`
  - [ ] `npx cap run ios` на актуальному Xcode: exit-код записано у звіт. Якщо `native-run` досі ламається, це факт для knowledge, а не FAIL
- **Для tester:** веб не зламано; збірка `mobile` йде лише в `dist/mobile`; платформні гілки лише через `core/platform.ts`.
- **Примітка PM (2026-11-14, етап 2a):** при поверненні до iOS врахувати теми PRP-015 (у PRP-015 це свідомо вирізано): колір статус-бару за `PrefsService.resolvedTheme()` (в Outlay — `core/native-shell.service.ts`, `syncStatusBar`, пакет `@capacitor/status-bar`), хук `html[data-native]` у `PrefsService`, `viewport-fit=cover` разом із safe-area в шапці, а також перевірка, що `public/theme-init.js` спрацьовує у WebView до першого кадру.
- **Контракт API:** без змін (додається лише CORS-origin).
- **Поза скоупом:** іконка, splash, TestFlight і App Store (недоступні з Personal Team).
