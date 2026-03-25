# RAG Client

Веб-клиент для корпоративной RAG-системы (Core API).

## Архитектура

```
rag-client/
├── backend/                    ASP.NET Core 9 — чистый API-сервер
│   ├── RagClient.Api/
│   │   ├── Configuration/      RagApiOptions (BaseUrl из appsettings)
│   │   ├── Controllers/        CollectionController, RagController
│   │   ├── Infrastructure/     IRagApiClient / RagApiClient (typed HttpClient)
│   │   ├── Middleware/         ApiKeyMiddleware (X-Api-Key → ApiKeyContext)
│   │   ├── Models/             Доменные модели и DTO
│   │   └── Services/           ICollectionService, IRagService
│   └── Dockerfile
├── frontend/                   Node.js (Express) — статика + прокси
│   ├── public/                 HTML / CSS / JS
│   ├── server.js               Express: /api/* → backend, статика → public/
│   └── Dockerfile
├── mock/                       Мок-сервер RAG API для локальной отладки
│   └── RagApiMock/
├── docker-compose.yml          Продакшн: backend + frontend
└── docker-compose.mock.yml     Оверрайд: добавляет mock-сервер
```

**Поток запроса:**
```
Браузер → frontend:3000/api/* → [Node.js proxy] → backend:8080/api/* → RAG API
```

**Поток API-ключа:**
```
localStorage → X-Api-Key header → ApiKeyMiddleware → ApiKeyContext (scoped) → RagApiClient → Authorization: Bearer <key>
```

## Реализованные методы

| Эндпоинт бэкенда | Метод | Проксирует |
|---|---|---|
| `GET  /api/collections` | Список коллекций с фильтрами | `GET /api/v1/collection` |
| `POST /api/rag/search` | RAG-поиск с ответом LLM | `POST /api/v1/rag/search` |
| `POST /api/rag/raw-search` | Сырой векторный поиск чанков | `POST /api/v1/rag/raw-search` |

---

## Запуск

### Локально (раздельные терминалы)

**Требуется:** .NET 9 SDK, Node.js 18+.

```powershell
# Терминал 1 — бэкенд
cd backend/RagClient.Api
dotnet run
# API доступен на http://localhost:5000

# Терминал 2 — фронтенд
cd frontend
npm install
npm start
# Интерфейс открывается на http://localhost:3000
```

### С моком (без доступа к внешнему API)

```powershell
# Терминал 1 — мок
cd mock/RagApiMock
dotnet run
# Слушает http://localhost:7070

# Терминал 2 — бэкенд → мок
cd backend/RagClient.Api
dotnet run --launch-profile http-mock
# API на http://localhost:5000, смотрит в мок

# Терминал 3 — фронтенд
cd frontend
npm start
# http://localhost:3000 → backend → mock
```

### Docker (продакшн)

```bash
docker compose up --build
# Открыть http://localhost:3000
```

Переопределить URL RAG API:
```bash
RAG_BASE_URL=https://rag.company.ru docker compose up --build
```
или через `.env`-файл: `RagApi__BaseUrl=https://rag.company.ru`

### Docker с моком

```bash
docker compose -f docker-compose.yml -f docker-compose.mock.yml up --build
# http://localhost:3000 → backend → mock (без выхода наружу)
```

---

## Руководство пользователя

### 1. Авторизация

При первом открытии введите Bearer-токен в поле **API Key** в правом верхнем углу и нажмите **Сохранить**. Ключ сохраняется в `localStorage` браузера и автоматически подставляется во все запросы.

---

### 2. Просмотр доступных коллекций

1. Перейдите на вкладку **Коллекции**.
2. Опционально задайте фильтры (название, ключ, сортировку, лимит).
3. Нажмите **Загрузить**.

В таблице отобразятся все доступные коллекции с их ключами, количеством документов и чанков, а также флагами включённых функций (гибридный поиск, реранкинг, query expansion).

Запомните **ключ коллекции** (`key`) — он понадобится для запросов.

---

### 3. Задать вопрос по коллекции (Чат)

1. Перейдите на вкладку **Чат (RAG Search)**.
2. В боковой панели **Параметры запроса**:
   - Нажмите кнопку ↻ рядом с дропдауном **Коллекция**, чтобы загрузить список.
   - Выберите коллекцию из списка **или** введите ключ вручную в поле ниже.
3. При необходимости скорректируйте параметры:
   - **Agent Role** — роль агента (например `assistant`, `analyst`).
   - **Max Documents** — сколько чанков передавать в контекст LLM.
   - **Min Similarity** — минимальный порог схожести (0–1).
   - **Max Tokens** — ограничение токенов ответа LLM.
   - **Temperature** — «творческость» модели (0 = детерминировано, ближе к 1 = разнообразнее).
   - **Контекст** — сколько предыдущих сообщений диалога передавать в запрос.
4. Введите вопрос в поле ввода внизу и нажмите **Enter** (или кнопку отправки).

Ответ LLM появится в диалоговом окне в формате Markdown. Последующие вопросы учитывают историю диалога — можно уточнять и переспрашивать.

Кнопка **Очистить чат** сбрасывает историю диалога.

---

### 4. Поиск чанков без LLM (Raw Search)

Raw Search возвращает сырые фрагменты документов, наиболее близкие к запросу по векторному расстоянию — без участия языковой модели. Полезно для отладки качества индексации.

1. Перейдите на вкладку **Raw Search**.
2. Укажите **Ключ коллекции** (обязательно).
3. Введите **поисковый запрос** (текстовый фрагмент или вопрос).
4. Задайте **Max Documents** (сколько чанков вернуть) и **Min Similarity** (порог отсечения).
5. Нажмите **Найти чанки**.

Для каждого результата отображается: исходный файл, порядковый номер чанка, текст и процент схожести с запросом.

---

## Конфигурация

| Переменная окружения | Где задаётся | Значение по умолчанию | Описание |
|---|---|---|---|
| `RagApi__BaseUrl` | backend | `https://rag.infra-prod.activebt.ru` | URL RAG Core API |
| `BACKEND_URL` | frontend | `http://localhost:5000` | URL бэкенда для Node.js-прокси |
| `PORT` | frontend | `3000` | Порт фронтенда |
| `ASPNETCORE_HTTP_PORTS` | backend | `8080` | Порт бэкенда (в Docker) |
