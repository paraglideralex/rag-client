using System.Text.Json;
using System.Text.Json.Serialization;

// ── JSON options: snake_case, ignore nulls ──────────────────────────────────
var jsonOpts = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
};

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();

// ── Helpers ─────────────────────────────────────────────────────────────────
bool IsAuthorized(HttpRequest req)
{
    var auth = req.Headers.Authorization.ToString();
    return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && auth.Length > 7;
}

IResult Unauthorized401() => Results.Json(
    new { Status = "error", Service = "mock", Errors = "Ошибка авторизации: Bearer токен не передан" },
    jsonOpts, statusCode: 401);

async Task Delay(int minMs = 80, int maxMs = 250) =>
    await Task.Delay(Random.Shared.Next(minMs, maxMs));

// ── GET /api/v1/collection ──────────────────────────────────────────────────
app.MapGet("/api/v1/collection", async (HttpRequest req) =>
{
    if (!IsAuthorized(req)) return Unauthorized401();
    await Delay();

    var nameFilter = req.Query["name"].ToString().ToLower();
    var keyFilter  = req.Query["key"].ToString().ToLower();

    var items = MockData.Collections
        .Where(c =>
            (string.IsNullOrEmpty(nameFilter) || c.Name.ToLower().Contains(nameFilter)) &&
            (string.IsNullOrEmpty(keyFilter)  || c.Key.ToLower().Contains(keyFilter)))
        .ToList();

    return Results.Json(new { Status = "ok", Service = "mock", Data = items }, jsonOpts);
});

// ── GET /api/v1/role ────────────────────────────────────────────────────────
app.MapGet("/api/v1/role", async (HttpRequest req) =>
{
    if (!IsAuthorized(req)) return Unauthorized401();
    await Delay();
    return Results.Json(new { Status = "ok", Service = "mock", Data = MockData.Roles }, jsonOpts);
});

// ── POST /api/v1/rag/search ─────────────────────────────────────────────────
app.MapPost("/api/v1/rag/search", async (HttpRequest req) =>
{
    if (!IsAuthorized(req)) return Unauthorized401();

    RagSearchBody? body;
    try { body = await req.ReadFromJsonAsync<RagSearchBody>(jsonOpts); }
    catch
    {
        return Results.Json(
            new { Status = "error", Service = "mock", Errors = "Невалидное тело запроса" },
            jsonOpts, statusCode: 400);
    }

    if (string.IsNullOrWhiteSpace(body?.CollectionKey))
        return Results.Json(new { Status = "error", Service = "mock", Errors = "collection_key обязателен" }, jsonOpts, statusCode: 400);
    if (string.IsNullOrWhiteSpace(body.Message))
        return Results.Json(new { Status = "error", Service = "mock", Errors = "message обязателен" }, jsonOpts, statusCode: 400);

    var collection = MockData.Collections.FirstOrDefault(c =>
        c.Key.Equals(body.CollectionKey, StringComparison.OrdinalIgnoreCase));
    if (collection is null)
        return Results.Json(
            new { Status = "error", Service = "mock", Errors = $"Коллекция '{body.CollectionKey}' не найдена" },
            jsonOpts, statusCode: 404);

    await Delay(800, 2200); // simulate LLM thinking

    var answer = MockData.GenerateAnswer(body.Message, collection.Name, body.AgentRole);
    return Results.Json(new { Status = "ok", Service = "mock", Data = answer }, jsonOpts);
});

// ── POST /api/v1/rag/raw-search ─────────────────────────────────────────────
app.MapPost("/api/v1/rag/raw-search", async (HttpRequest req) =>
{
    if (!IsAuthorized(req)) return Unauthorized401();

    RawSearchBody? body;
    try { body = await req.ReadFromJsonAsync<RawSearchBody>(jsonOpts); }
    catch
    {
        return Results.Json(
            new { Status = "error", Service = "mock", Errors = "Невалидное тело запроса" },
            jsonOpts, statusCode: 400);
    }

    if (string.IsNullOrWhiteSpace(body?.CollectionKey))
        return Results.Json(new { Status = "error", Service = "mock", Errors = "collection_key обязателен" }, jsonOpts, statusCode: 400);
    if (string.IsNullOrWhiteSpace(body.Message))
        return Results.Json(new { Status = "error", Service = "mock", Errors = "message обязателен" }, jsonOpts, statusCode: 400);

    var collection = MockData.Collections.FirstOrDefault(c =>
        c.Key.Equals(body.CollectionKey, StringComparison.OrdinalIgnoreCase));
    if (collection is null)
        return Results.Json(
            new { Status = "error", Service = "mock", Errors = $"Коллекция '{body.CollectionKey}' не найдена" },
            jsonOpts, statusCode: 404);

    await Delay(200, 600);

    var limit  = Math.Clamp(body.MaxDocuments ?? 5, 1, 20);
    var minSim = body.MinSimilarity ?? 0.0;
    var chunks = MockData.GenerateChunks(body.Message, body.CollectionKey, limit, minSim);

    return Results.Json(new { Status = "ok", Service = "mock", Data = chunks }, jsonOpts);
});

// ── Catch-all 404 ────────────────────────────────────────────────────────────
app.MapFallback(ctx =>
{
    ctx.Response.StatusCode = 404;
    return ctx.Response.WriteAsJsonAsync(
        new { Status = "error", Service = "mock", Errors = $"Endpoint не найден: {ctx.Request.Method} {ctx.Request.Path}" },
        jsonOpts);
});

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       RAG API Mock  ·  http://localhost:7070         ║");
Console.WriteLine("╠══════════════════════════════════════════════════════╣");
Console.WriteLine("║  GET  /api/v1/collection                             ║");
Console.WriteLine("║  GET  /api/v1/role                                   ║");
Console.WriteLine("║  POST /api/v1/rag/search      (задержка 0.8–2.2 с)   ║");
Console.WriteLine("║  POST /api/v1/rag/raw-search  (задержка 0.2–0.6 с)   ║");
Console.WriteLine("║                                                      ║");
Console.WriteLine("║  Bearer токен: любая непустая строка                 ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");

app.Run();

// ═══════════════════════════════════════════════════════════════════════════
// Request / response contracts
// ═══════════════════════════════════════════════════════════════════════════

record RagSearchBody(
    string? CollectionKey,
    string? Message,
    string? AgentRole,
    int?    MaxDocuments,
    int?    MaxTokens,
    double? MinSimilarity,
    double? Temperature,
    bool    Stream = false,
    List<ContextMessage>? ContextMessages = null);

record RawSearchBody(
    string? CollectionKey,
    string? Message,
    int?    MaxDocuments,
    double? MinSimilarity);

record ContextMessage(string Role, string Content);

// ═══════════════════════════════════════════════════════════════════════════
// Mock data & generators
// ═══════════════════════════════════════════════════════════════════════════

static class MockData
{
    // ── Roles ──────────────────────────────────────────────────────────────
    public static readonly List<object> Roles =
    [
        new { Id = "assistant", Name = "Помощник",           Description = "Базовая роль ассистента",         Prompt = "Ты полезный и лаконичный помощник." },
        new { Id = "analyst",   Name = "Аналитик",            Description = "Роль аналитика данных",           Prompt = "Ты аналитик. Отвечай структурированно, используй таблицы и списки." },
        new { Id = "legal",     Name = "Юридический советник", Description = "Роль юридического консультанта", Prompt = "Ты юридический консультант. Отвечай формально, ссылаясь на нормы." },
    ];

    // ── Collections ────────────────────────────────────────────────────────
    public static readonly List<CollectionRecord> Collections =
    [
        new(1, "product_docs",  "Документация по продукту",    "Внутренняя документация по всем модулям продукта.",              true,  true,  false, 47, 1243, 8_388_608, 1739188800000L, 1739275200000L),
        new(2, "hr_policy",     "HR Политики",                 "Регламенты, политики и процедуры кадрового отдела.",             false, false, true,  12,  318, 1_048_576, 1739102400000L, 1739188800000L),
        new(3, "tech_specs",    "Технические спецификации",    "Архитектурные схемы, ADR и технические спецификации систем.",    true,  false, false, 31,  876, 5_242_880, 1738944000000L, 1739102400000L),
        new(4, "faq_support",   "FAQ поддержки",               "Часто задаваемые вопросы и ответы службы поддержки.",            false, true,  true,   8,  204,   524_288, 1738857600000L, 1738944000000L),
        new(5, "legal_base",    "Нормативная база",             "Нормативные документы, договоры и регуляторные требования.",     false, false, false, 23,  654, 3_145_728, 1738771200000L, 1738857600000L),
    ];

    // ── Chunks per collection ──────────────────────────────────────────────
    private static readonly Dictionary<string, string[]> ChunkTexts = new()
    {
        ["product_docs"] =
        [
            "Модуль аутентификации поддерживает OAuth 2.0 и JWT-токены. Срок жизни access-токена — 15 минут, refresh-токена — 30 дней. Для продления сессии необходимо вызвать endpoint POST /auth/refresh с действующим refresh-токеном.",
            "Архитектура системы построена на микросервисах. Взаимодействие между сервисами осуществляется через gRPC (синхронные вызовы) и RabbitMQ (асинхронные события). Каждый сервис хранит данные в собственной базе данных.",
            "Процесс деплоя: сборка Docker-образа → прогон тестов в CI → push в registry → rolling update в Kubernetes. Rollback выполняется автоматически при падении health-check более 3 раз подряд.",
            "API versioning: все endpoints версионируются через URL-префикс /v{N}/. Устаревшие версии поддерживаются минимум 6 месяцев после выхода новой. Список deprecated endpoints публикуется в CHANGELOG.",
            "Логирование ведётся через structured logging (Serilog). Уровни: DEBUG (только dev), INFO (prod), WARN, ERROR. Трассировка запросов — OpenTelemetry + Jaeger. Логи агрегируются в Loki.",
            "Rate limiting: по умолчанию 100 запросов/мин на IP. Для B2B-клиентов лимит 1000 запросов/мин на API-ключ. При превышении — HTTP 429 с заголовком Retry-After.",
        ],
        ["hr_policy"] =
        [
            "Испытательный срок составляет 3 месяца. В этот период сотрудник работает по стандартному графику и имеет право на оплачиваемый отпуск пропорционально отработанному времени.",
            "Удалённая работа разрешена не более 3 дней в неделю. Для постоянного перевода на remote-формат необходимо согласование с непосредственным руководителем и HR-отделом.",
            "Оценка производительности проводится дважды в год: в июне и декабре. По результатам формируется ИПР (индивидуальный план развития) на следующее полугодие.",
            "Компания компенсирует обучение в размере до 50 000 руб./год при условии, что тематика курса связана с профессиональной деятельностью сотрудника.",
            "Больничный лист оформляется через портал госуслуг и передаётся в HR в течение 2 рабочих дней с момента закрытия. Оплата производится в соответствии с трудовым законодательством РФ.",
        ],
        ["tech_specs"] =
        [
            "База данных: PostgreSQL 15. Подключение через connection pool (pgBouncer, max_pool_size=100). Репликация: 1 primary + 2 read replicas в разных availability zones. Backup — ежедневный snapshot в S3 с retention 30 дней.",
            "Кэширование: Redis Cluster (3 шарда, 1 replica per shard). TTL для сессий — 1 час, для справочных данных — 24 часа. Инвалидация по событиям через Redis Pub/Sub.",
            "Балансировщик: nginx с алгоритмом least_conn. Health check каждые 5 секунд, таймаут 3 секунды. Circuit breaker срабатывает на 5 последовательных ошибках, время восстановления — 30 секунд.",
            "Мониторинг: Prometheus (scrape interval 15s) + Grafana. Alerting через PagerDuty. SLA: 99.9% uptime, P95 latency < 200 ms, P99 < 500 ms, error rate < 0.1%.",
            "Message broker: RabbitMQ 3.12, quorum queues. Dead-letter queue для неуспешных сообщений. Retry policy: 3 попытки с exponential backoff (1s, 5s, 30s).",
        ],
        ["faq_support"] =
        [
            "Как сбросить пароль: страница входа → «Забыли пароль?» → введите email → получите письмо со ссылкой для сброса (ссылка действительна 1 час). Новый пароль должен содержать ≥8 символов, заглавную букву и цифру.",
            "Ошибка «403 Forbidden»: у вашей роли нет нужных разрешений на данный ресурс. Обратитесь к администратору системы для назначения требуемых прав. Изменения вступают в силу немедленно без перезахода.",
            "Экспорт данных доступен в форматах CSV, XLSX и JSON. Раздел «Отчёты» → «Экспорт» → выберите период и формат → «Скачать». Максимальный объём выгрузки — 100 000 строк.",
            "Двухфакторная аутентификация: поддерживаются TOTP (Google Authenticator, Authy) и SMS. Настройки → Безопасность → 2FA. При потере устройства используйте резервные коды (генерируются при подключении 2FA).",
        ],
        ["legal_base"] =
        [
            "Обработка персональных данных осуществляется в соответствии с 152-ФЗ «О персональных данных». Согласие субъекта фиксируется в электронном виде и хранится не менее 3 лет после прекращения обработки.",
            "Договор оферты акцептуется нажатием кнопки «Принять условия». Акцепт означает полное согласие с версией пользовательского соглашения, актуальной на момент регистрации.",
            "Срок хранения финансовой документации — 5 лет с момента создания. По истечении срока документы уничтожаются по акту в соответствии с внутренним регламентом документооборота.",
            "Претензии по качеству услуг принимаются в письменной форме в течение 30 дней с момента обнаружения нарушения. Срок рассмотрения претензии — 10 рабочих дней.",
        ],
    };

    private static readonly string[] FallbackChunks =
    [
        "Данный раздел содержит общие сведения о системе. Для получения подробной информации обратитесь к соответствующему разделу документации.",
        "Функционал находится в стадии разработки. Планируемая дата выхода — следующий квартал. Актуальный статус отслеживается в roadmap.",
        "Интеграция с внешними системами осуществляется через REST API. Документация по интеграции доступна по запросу у команды разработки.",
    ];

    private static string[] ChunksFor(string collectionKey) =>
        ChunkTexts.TryGetValue(collectionKey, out var c) ? c : FallbackChunks;

    // ── Mock LLM answer ────────────────────────────────────────────────────
    public static string GenerateAnswer(string question, string collectionName, string? agentRole)
    {
        var rolePrefix = (agentRole ?? "assistant") switch
        {
            "analyst" => "С аналитической точки зрения: ",
            "legal"   => "С правовой точки зрения: ",
            _         => string.Empty
        };

        return
            "# Ответ по коллекции «" + collectionName + "»\n\n" +
            rolePrefix + "На основе найденных материалов в базе знаний.\n\n" +
            "## По вашему запросу\n\n" +
            "Вы спросили: **«" + question + "»**\n\n" +
            "Согласно документации, данная тема охватывает несколько ключевых аспектов:\n\n" +
            "1. **Первый аспект** — система поддерживает несколько режимов работы, каждый из которых оптимизирован для конкретных сценариев.\n" +
            "2. **Второй аспект** — конфигурация выполняется через переменные окружения или файл настроек, что обеспечивает гибкость развёртывания.\n" +
            "3. **Третий аспект** — для корректной работы убедитесь, что все зависимости установлены и сервисы запущены в правильном порядке.\n\n" +
            "## Пример конфигурации\n\n" +
            "```json\n" +
            "{\n" +
            "  \"config\": \"example\",\n" +
            "  \"enabled\": true,\n" +
            "  \"threshold\": 0.75\n" +
            "}\n" +
            "```\n\n" +
            "## Дополнительно\n\n" +
            "Если у вас остались вопросы, рекомендую обратиться к разделу «Часто задаваемые вопросы» или связаться с командой поддержки.\n\n" +
            "> *Это тестовый ответ от mock-сервера. В реальной системе здесь будет ответ LLM, " +
            "сгенерированный на основе найденных чанков из коллекции «" + collectionName + "».*";
    }

    // ── Mock raw chunks ────────────────────────────────────────────────────
    public static List<object> GenerateChunks(string query, string collectionKey, int limit, double minSimilarity)
    {
        var texts = ChunksFor(collectionKey);
        var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var collectionId = Collections.FirstOrDefault(c => c.Key == collectionKey)?.Id ?? 1;

        var results = texts
            .Select((text, idx) =>
            {
                var wordHits = queryWords.Count(w => text.ToLower().Contains(w));
                var baseSim = 0.50 + wordHits * 0.07 + Random.Shared.NextDouble() * 0.15;
                var sim = Math.Round(Math.Min(baseSim, 0.98), 4);
                return (idx, text, sim);
            })
            .Where(x => x.sim >= minSimilarity)
            .OrderByDescending(x => x.sim)
            .Take(limit)
            .Select((x, rank) => (object)new
            {
                Id         = collectionId * 1000 + x.idx,
                FileId     = collectionId * 100 + (x.idx / 2),
                FileName   = collectionKey + "_doc_" + (x.idx / 2 + 1) + ".pdf",
                Index      = x.idx % 3,
                Content    = x.text,
                Similarity = x.sim,
                Meta       = (object?)null,
            })
            .ToList();

        return results;
    }
}

// ── Domain records ───────────────────────────────────────────────────────────
record CollectionRecord(
    int    Id,
    string Key,
    string Name,
    string Description,
    bool   HybridSearchEnabled,
    bool   RerankingEnabled,
    bool   QueryExpansionEnabled,
    int    DocumentsCount,
    int    ChunksCount,
    long   Size,
    long   CreatedAt,
    long   UpdatedAt);
