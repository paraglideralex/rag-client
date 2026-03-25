# RAG Client

Веб-клиент для корпоративной RAG-системы (Core API). Бэкенд на ASP.NET Core 10, фронт на чистом HTML/CSS/JS.

## Архитектура

```
src/RagClient.Api/
├── Configuration/       — RagApiOptions (BaseUrl из appsettings)
├── Controllers/         — тонкий HTTP-слой, делегирует сервисам
├── Infrastructure/      — IRagApiClient / RagApiClient (типизированный HttpClient)
├── Middleware/          — ApiKeyMiddleware (извлекает X-Api-Key → ApiKeyContext)
├── Models/              — доменные модели и DTO запросов/ответов
├── Services/            — бизнес-логика (ICollectionService, IRagService)
└── wwwroot/             — статический фронт
```

Поток API-ключа: `localStorage (браузер) → X-Api-Key header → ApiKeyMiddleware → ApiKeyContext (scoped) → RagApiClient → Authorization: Bearer <key>`.

## Реализованные методы

| Эндпоинт нашего бэка | Метод | Проксирует |
|---|---|---|
| `GET /api/collections` | Список коллекций с фильтрами | `GET /api/v1/collection` |
| `POST /api/rag/search` | RAG-поиск с ответом LLM | `POST /api/v1/rag/search` |
| `POST /api/rag/raw-search` | Сырой векторный поиск чанков | `POST /api/v1/rag/raw-search` |

## Запуск

### Локально (отладка)

Требуется .NET 9 SDK (или 10+, тогда поменять `<TargetFramework>` в `.csproj`).

```bash
cd src/RagClient.Api
dotnet run
# Открыть http://localhost:5000
```

### Docker

```bash
docker compose up --build
# Открыть http://localhost:8080
```

Переменная `RagApi__BaseUrl` переопределяет базовый URL API (по умолчанию `https://rag.infra-prod.activebt.ru`).

## Использование

1. Введите Bearer-токен в поле **API Key** в шапке и нажмите **Сохранить** — ключ сохраняется в `localStorage`.
2. Вкладка **Чат** — задавайте вопросы в стиле ChatGPT. В левой панели настройте: коллекцию, Agent Role, лимиты, температуру. История диалога автоматически передаётся как `context_messages`.
3. Вкладка **Коллекции** — просмотр доступных коллекций с фильтрацией и сортировкой.
4. Вкладка **Raw Search** — сырой векторный поиск без LLM; возвращает чанки с % схожести.
