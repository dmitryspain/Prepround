# Специфікація Prepround (MVP)

> Джерело правди для поведінки продукту. Складено з обговорення з власником 2026-11-13.
> Змінювати лише за рішенням власника: PM пропонує правку, головна сесія вносить.

## 1. Навіщо

Людина готується до співбесіди й хоче перевірити себе саме з тих тем, які їй потрібні, у потрібній пропорції і на потрібному рівні. Наприклад: «30 питань, Middle + Senior: 15 по .NET, 10 по SQL, 5 по React». Після тесту вона бачить, які теми провисають, і читає пояснення до кожного питання.

Початкова аудиторія — розробники (Backend, Frontend). Модель даних має з самого початку витримувати інші галузі (DevOps, дизайн, медицина, наука) без зміни коду.

## 2. Модель каталогу

```
Галузь (Field), наприклад IT
 ├─ шкала рівнів: упорядкований список (IT: Junior → Middle → Senior)
 ├─ напрями (Direction): Backend, Frontend
 │   └─ стеки (Stack): .NET, Java…, тобто упорядковані набори id тем за замовчуванням
 └─ теми (Topic): каталог галузі, спільний для всіх стеків
     └─ питання (Question): рівень зі шкали галузі, тип, варіанти, пояснення
```

- **Рівні** — дані. Порядок у шкалі задає ранг (для сортування й відображення). Щоб додати Architect, достатньо дописати рядок у `field.yaml`, і старі питання цього не відчують. Інша галузь може мати іншу шкалу, наприклад «Аспірант → Кандидат наук → Доктор наук».
- **Тема** належить галузі. Стек лише посилається на теми, тому одну тему (SQL, Docker, JavaScript) використовують кілька стеків, навіть із різних напрямів.
- У питання один рівень.

## 3. Сценарій користувача

### 3.1 Налаштування тесту

1. **Галузь.** Якщо галузь одна, крок не показуємо.
2. **Напрям і стек.** Вибір стеку підставляє його теми. Далі людина може прибрати будь-яку тему або додати тему з каталогу галузі (пошук за назвою).
3. **Рівні:** один або кілька, показуються в порядку шкали. За замовчуванням вибрано всі.
4. **Загальна кількість питань:** від 10 до 100, за замовчуванням 20.
5. **Розподіл по темах:**
   - щойно змінюється набір тем або загальна кількість, питання діляться порівну, а остачу отримують перші теми: 30 на 4 теми дає 8/8/7/7;
   - кожну тему можна змінити вручну, мінімум 1;
   - **максимум по темі** дорівнює кількості активних питань теми на вибраних рівнях. Якщо після зміни рівнів поточне значення перевищує максимум, воно знижується до максимуму;
   - показуємо «Розподілено X з Y». Кнопка «Почати» активна лише тоді, коли X = Y, кожна тема має ≥ 1 питання і жодна не перевищує свій максимум;
   - якщо загальна кількість більша за суму максимумів вибраних тем, показуємо підказку: додайте тем або рівнів;
   - тему без жодного питання на вибраних рівнях показуємо неактивною з позначкою «0 питань». Неактивна тема не бере участі в розподілі, не потрапляє в запит і не блокує «Почати» (правило «≥ 1» стосується лише активних тем).
6. **Таймер:** за замовчуванням увімкнений, 60 с × кількість питань (20 питань → 20 хв). Вимикається перемикачем. Значення 60 с — константа в конфігу фронту.

### 3.2 Проходження

- Одне питання на екран. Видно номер «7 / 30», тему й рівень. Є навігація «Назад / Далі» і сітка номерів, щоб перейти до будь-якого питання. На сітці видно стан питання: без відповіді, з відповіддю, перевірено правильно, перевірено неправильно.
- `single` показуємо radio-кнопками, `multiple` — чекбоксами з підписом «Кілька правильних відповідей».
- Порядок варіантів перемішаний (це робить сервер, див. 5.2).
- **«Перевірити»** активна, коли вибрано хоча б один варіант. Вона показує «Правильно» або «Неправильно», підсвічує правильні варіанти і показує пояснення. Відповідь після цього **заблокована**.
- Без перевірки відповідь можна змінювати до кінця тесту.
- **«Завершити тест»** доступна завжди. Якщо є питання без відповіді, перед завершенням з'являється підтвердження з їх кількістю (це не системний `confirm()`, а елемент інтерфейсу).
- **Таймер** показує решту часу. Коли час вийшов, тест завершується автоматично.
- Якщо закрити або перезавантажити сторінку посеред тесту, прогрес не зберігається (MVP). Перед виходом браузер показує стандартне попередження `beforeunload`.

### 3.3 Оцінювання

- `single`: правильно, якщо вибрано правильний варіант.
- `multiple`: правильно лише тоді, коли множина вибраних варіантів точно збігається з множиною правильних. Часткових балів немає.
- Питання без відповіді рахується неправильним.
- Результат = правильні / усі питання тесту, у відсотках, округлення до цілого.

### 3.4 Результат

- Загальний відсоток і «X з Y», витрачений час.
- Розбивка по темах «тема: правильні / усі», від найслабшої теми.
- Розбір: кожне питання з відповіддю людини, правильними варіантами й поясненням. Можна фільтрувати «лише помилки».
- «Пройти ще раз з тими самими налаштуваннями» і «Нові налаштування».

### 3.5 Історія і «вже бачені» (лише на пристрої)

- Після завершення тесту на пристрої зберігається результат: дата (UTC), налаштування, відсоток, розбивка по темах, відповіді (id питань і вибрані варіанти). Окремо зберігаються id усіх показаних питань («вже бачені»).
- Екран «Історія» показує список минулих тестів, новіші вгорі, і дає відкрити результат.
- Кнопка «Очистити історію» стирає результати й «вже бачені». Підтвердження робимо елементом інтерфейсу.
- Під час створення тесту клієнт передає id «вже бачених» питань вибраних тем, і сервер віддає перевагу іншим.

### 3.6 Оформлення (лише на пристрої)

Рішення власника 2026-11-14 («хочу красоту, як в Outlay»).

- Теми: **Aurora** (темна), **Daylight** (світла), **Terminal** (темна, моноширинна) і **«Авто»**: темна система → Aurora, світла → Daylight, стежить за зміною системної теми. «Авто» — за замовчуванням.
- Акцент: власний колір теми, один із 8 відтінків або довільний відтінок повзунком.
- Сторінка «Налаштування» з прев'ю тем. Перемикання анімоване (коло від кнопки), якщо браузер це вміє і система не просить зменшити рух.
- Вибір зберігається на пристрої й не стирається кнопкою «Очистити історію».

## 4. Каталог IT для старту

Шкала рівнів IT: `junior` (Junior) → `middle` (Middle) → `senior` (Senior).

### 4.1 Напрями і стеки (по 10 тем; ★ — спільна тема)

**Backend**

| Стек (id) | Теми (id) |
|---|---|
| .NET (C#) — `dotnet` | `csharp`, `dotnet-clr`, `dotnet-async`, `aspnet-core`, `ef-core`, ★`sql`, ★`architecture`, ★`messaging`, ★`azure`, ★`docker-k8s` |
| Java — `java` | `java-core`, `jvm`, `java-concurrency`, `spring-boot`, `hibernate-jpa`, ★`sql`, ★`architecture`, ★`messaging`, ★`aws`, ★`docker-k8s` |
| Python — `python` | `python-core`, `python-async`, `django`, `fastapi`, `sqlalchemy`, `celery`, `pytest`, ★`sql`, ★`nosql`, ★`docker-k8s` |
| Node.js — `nodejs` | ★`javascript`, ★`typescript`, `nodejs-runtime`, `express-fastify`, `nestjs`, `prisma-typeorm`, ★`sql`, ★`nosql`, ★`api-design`, ★`docker-k8s` |
| Go — `go` | `go-lang`, `go-concurrency`, `go-runtime`, `go-http`, `go-db`, ★`api-design`, ★`sql`, ★`messaging`, ★`architecture`, ★`docker-k8s` |
| PHP — `php` | `php-lang`, `laravel`, `symfony`, `php-orm`, `composer-psr`, `phpunit`, ★`sql`, ★`nosql`, ★`api-design`, ★`architecture` |

**Frontend**

| Стек (id) | Теми (id) |
|---|---|
| React — `react` | ★`javascript`, ★`typescript`, ★`html-css`, `react-core`, `react-hooks`, `react-state`, `nextjs`, `react-data-routing`, ★`frontend-testing`, ★`web-performance` |
| Angular — `angular` | ★`javascript`, ★`typescript`, ★`html-css`, `angular-components`, `angular-di`, `angular-change-detection`, `rxjs`, `ngrx`, `angular-routing-forms`, ★`frontend-testing` |
| Vue — `vue` | ★`javascript`, ★`typescript`, ★`html-css`, `vue-core`, `vue-composition-api`, `pinia`, `vue-router`, `nuxt`, ★`frontend-testing`, ★`web-performance` |

### 4.2 Теми (61): id → назва → що покриває

**Спільні (13)**

| id | Назва | Покриває |
|---|---|---|
| `sql` | SQL і реляційні БД | запити, JOIN, індекси, транзакції, рівні ізоляції, нормалізація, плани виконання |
| `nosql` | NoSQL і кеш | Redis, MongoDB, моделі даних, кешування, TTL, консистентність |
| `api-design` | API-дизайн | REST, коди статусів, версіонування, ідемпотентність, gRPC і Protobuf, GraphQL, WebSocket |
| `messaging` | Брокери повідомлень | Kafka, RabbitMQ, гарантії доставки, outbox, ідемпотентні консюмери |
| `architecture` | Архітектура й патерни | SOLID, GoF-патерни, Clean Architecture, DDD, мікросервіси, system design |
| `docker-k8s` | Docker і Kubernetes | образи, шари, compose, поди, деплойменти, сервіси, probes, ресурси |
| `aws` | AWS | EC2, S3, Lambda, RDS, SQS/SNS, IAM, VPC |
| `azure` | Azure | App Service, Functions, Azure SQL, Service Bus, Key Vault, Entra ID |
| `javascript` | JavaScript | типи, замикання, прототипи, this, event loop, проміси, async/await, модулі |
| `typescript` | TypeScript | типи й інтерфейси, generics, union/intersection, narrowing, utility types, strict |
| `html-css` | HTML і CSS | семантика, доступність, box model, flexbox, grid, каскад і специфічність, адаптивність |
| `frontend-testing` | Тестування фронтенду | юніт-тести (Vitest, Jest), Testing Library, e2e (Playwright), моки |
| `web-performance` | Продуктивність і браузер | рендеринг, reflow/repaint, Core Web Vitals, lazy loading, кешування, бандл |

**.NET (5)**

| id | Назва | Покриває |
|---|---|---|
| `csharp` | C# | типи, generics, LINQ, records, pattern matching, nullable reference types, делегати й події |
| `dotnet-clr` | CLR, пам'ять і GC | value/reference types, stack/heap, GC і покоління, IDisposable, Span |
| `dotnet-async` | Async і багатопотоковість | Task, async/await, SynchronizationContext, lock, TPL, Channels, дедлоки |
| `aspnet-core` | ASP.NET Core | middleware, DI і lifetimes, маршрутизація, Minimal API, автентифікація, конфігурація |
| `ef-core` | Entity Framework Core | DbContext, відстеження змін, міграції, завантаження зв'язків, N+1, продуктивність |

**Java (5)**

| id | Назва | Покриває |
|---|---|---|
| `java-core` | Java Core | ООП, колекції, generics, Stream API, винятки, records, equals/hashCode |
| `jvm` | JVM, пам'ять і GC | heap/stack, class loading, збирачі сміття, JIT, тюнінг |
| `java-concurrency` | Багатопотоковість у Java | Thread, ExecutorService, CompletableFuture, synchronized, java.util.concurrent, virtual threads |
| `spring-boot` | Spring Boot | IoC/DI, біни і скоупи, автоконфігурація, Spring MVC, Spring Security, профілі |
| `hibernate-jpa` | Hibernate і JPA | сутності, зв'язки, lazy/eager, N+1, транзакції, кеш першого/другого рівня |

**Python (7)**

| id | Назва | Покриває |
|---|---|---|
| `python-core` | Python Core | типи, mutable/immutable, генератори, декоратори, контекст-менеджери, ООП, dataclasses |
| `python-async` | Async, GIL і конкурентність | asyncio, GIL, threading vs multiprocessing, event loop |
| `django` | Django і DRF | моделі, ORM, міграції, views, middleware, серіалізатори DRF |
| `fastapi` | FastAPI | маршрути, Pydantic, залежності, async-ендпоінти, OpenAPI |
| `sqlalchemy` | SQLAlchemy | Core vs ORM, сесії, зв'язки, завантаження, Alembic |
| `celery` | Celery і фонові задачі | брокери, воркери, ретраї, ідемпотентність, періодичні задачі |
| `pytest` | Тестування в Python | pytest, фікстури, параметризація, моки |

**Node.js (4)**

| id | Назва | Покриває |
|---|---|---|
| `nodejs-runtime` | Node.js runtime | event loop і фази, libuv, streams, buffers, worker threads, модулі |
| `express-fastify` | Express і Fastify | маршрути, middleware, обробка помилок, валідація |
| `nestjs` | NestJS | модулі, провайдери, DI, guards, pipes, interceptors |
| `prisma-typeorm` | Prisma і TypeORM | схеми, міграції, зв'язки, транзакції |

**Go (5)**

| id | Назва | Покриває |
|---|---|---|
| `go-lang` | Мова Go | типи, slices і maps, інтерфейси, структури, помилки, generics |
| `go-concurrency` | Горутини й канали | goroutines, channels, select, sync, context, data race |
| `go-runtime` | Runtime Go | scheduler (GMP), GC, escape analysis, пам'ять |
| `go-http` | net/http і фреймворки | net/http, middleware, Gin/Echo, graceful shutdown |
| `go-db` | Робота з БД у Go | database/sql, sqlx, GORM, пул з'єднань, транзакції |

**PHP (6)**

| id | Назва | Покриває |
|---|---|---|
| `php-lang` | PHP 8 | типи, ООП, traits, enums, readonly, match, винятки |
| `laravel` | Laravel | маршрути, контролери, middleware, черги, service container |
| `symfony` | Symfony | бандли, DI-контейнер, події, форми, конфігурація |
| `php-orm` | Eloquent і Doctrine | Active Record vs Data Mapper, зв'язки, міграції, N+1 |
| `composer-psr` | Composer і PSR | автозавантаження, версії залежностей, PSR-4/7/12/15 |
| `phpunit` | Тестування в PHP | PHPUnit, моки, data providers |

**React (5)**

| id | Назва | Покриває |
|---|---|---|
| `react-core` | React: компоненти й рендер | JSX, props/state, reconciliation, keys, Virtual DOM, StrictMode |
| `react-hooks` | Hooks | useState, useEffect, useMemo/useCallback, useRef, власні хуки, правила хуків |
| `react-state` | Стан у React | Context, Redux Toolkit, Zustand, підйом стану |
| `nextjs` | Next.js | App Router, SSR/SSG/ISR, Server Components, маршрутизація |
| `react-data-routing` | Дані й роутинг | TanStack Query, React Router, кешування запитів, Suspense |

**Angular (6)**

| id | Назва | Покриває |
|---|---|---|
| `angular-components` | Компоненти й шаблони | standalone, input/output, життєвий цикл, control flow, content projection |
| `angular-di` | DI і сервіси | інжектори, providedIn, токени, ієрархія, inject() |
| `angular-change-detection` | Change detection і Signals | zone.js vs zoneless, OnPush, signal/computed/effect |
| `rxjs` | RxJS | Observable, оператори, subjects, higher-order mapping, відписка |
| `ngrx` | NgRx | Store, actions, reducers, effects, selectors, SignalStore |
| `angular-routing-forms` | Роутинг і форми | роутер, guards, resolvers, lazy loading, reactive forms, валідація |

**Vue (5)**

| id | Назва | Покриває |
|---|---|---|
| `vue-core` | Vue: реактивність і шаблони | реактивність, директиви, компоненти, props/emits, slots |
| `vue-composition-api` | Composition API | setup, ref/reactive, computed, watch, composables, vs Options API |
| `pinia` | Pinia | stores, state/getters/actions, плагіни, порівняння з Vuex |
| `vue-router` | Vue Router | маршрути, guards, lazy loading, вкладені маршрути |
| `nuxt` | Nuxt | SSR/SSG, файлова маршрутизація, data fetching, модулі |

## 5. Контракт API (MVP)

Усі ендпоінти анонімні. JSON у camelCase. Помилки повертаються як `400` з тілом `{ "code": "Area.Code", "message": "Текст українською." }`.

### 5.1 `GET /api/catalog`

Повертає лише активні сутності. Порядок рівнів відповідає шкалі, порядок напрямів, стеків і тем стеку — файлу. Теми галузі сортуються за назвою; порядок задає сервер (`StringComparer.InvariantCulture`: латиниця без урахування регістру, потім кирилиця), клієнт його не змінює. У `topicIds` стеку потрапляють лише активні теми.

```json
{
  "fields": [{
    "id": "it", "title": "IT",
    "levels": [{ "id": "junior", "title": "Junior", "rank": 1 }],
    "directions": [{ "id": "backend", "title": "Backend",
      "stacks": [{ "id": "dotnet", "title": ".NET (C#)", "topicIds": ["csharp", "ef-core"] }] }],
    "topics": [{ "id": "ef-core", "title": "Entity Framework Core", "description": "…",
      "questionCounts": { "junior": 4, "middle": 4, "senior": 4 } }]
  }]
}
```

`questionCounts` містить кількість активних питань теми за id рівня. Рівнів без питань у ньому немає.

### 5.2 `POST /api/tests`

```json
{
  "fieldId": "it",
  "levelIds": ["middle", "senior"],
  "topics": [{ "topicId": "ef-core", "count": 5 }, { "topicId": "sql", "count": 5 }],
  "excludeQuestionIds": ["ef-core-001"]
}
```

Відповідь `200`:

```json
{
  "questions": [{
    "id": "ef-core-007", "topicId": "ef-core", "levelId": "middle", "type": "single",
    "text": "…", "code": { "lang": "csharp", "source": "…" },
    "options": [{ "key": 2, "text": "…", "isCorrect": true }],
    "explanation": "…"
  }]
}
```

Правила:
- **Валідація** (порушення → `400` зі своїм кодом помилки, без часткових результатів):
  - `fieldId` існує;
  - `levelIds` непорожній, рівні належать галузі, без дублікатів;
  - `topics` непорожній, теми існують, активні, належать галузі, без дублікатів;
  - кожен `count` ≥ 1;
  - сума `count` у межах 10–100;
  - `count` ≤ кількості активних питань теми на вибраних рівнях;
  - `excludeQuestionIds` має не більше 10 000 елементів.
- **Вибір по кожній темі:** серед активних питань теми на вибраних рівнях спершу береться випадкова підмножина тих, яких немає в `excludeQuestionIds`. Якщо їх не вистачає, решта добирається випадково з виключених. Дублікатів у тесті немає.
- **Порядок:** питання всього тесту перемішані між темами. Варіанти кожного питання перемішані.
- **`key` варіанта** — це його індекс у файлі (з 0). Так відповідь можна зіставити з питанням після перемішування.
- `code` є необов'язковим (`null`, якщо блоку коду немає).
- **Rate limiting:** не більше 30 запитів на хвилину з однієї IP-адреси, при перевищенні `429`.

### 5.3 `GET /api/health`

Відповідає `200` і `{ "status": "ok" }`. Базу не перевіряє: API стартує й без неї.

## 6. Поза MVP (поки не робимо)

- акаунти й синхронізація історії між пристроями;
- адмінка для питань;
- питання від користувачів;
- інші галузі та напрями (DevOps, QA, мобільна розробка, дизайн);
- англійська версія інтерфейсу й контенту;
- підсвітка синтаксису;
- збереження незавершеного тесту;
- власний домен prepround.app (MVP з 2026-11-19 живе на безкоштовних піддоменах `prepround.pages.dev` і `prepround-api.onrender.com`, ADR 005);
- iOS-застосунок (Capacitor): відкладено рішенням власника 2026-11-14, без дати (беклог, PRP-008).
