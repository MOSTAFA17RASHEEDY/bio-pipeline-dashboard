using BioPipeline.Api.Data;
using BioPipeline.Api.Endpoints;
using BioPipeline.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Pipeline configuration ------------------------------------------------
// PipelineDir/RunsStorageDir come in as relative paths from appsettings.json
// (so the repo stays portable across machines) and get resolved to absolute
// paths once, here, against the API's content root.
builder.Services.Configure<PipelineOptions>(builder.Configuration.GetSection("Pipeline"));
builder.Services.PostConfigure<PipelineOptions>(opts =>
{
    opts.PipelineDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, opts.PipelineDir));
    opts.RunsStorageDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, opts.RunsStorageDir));
});

// --- Database ---------------------------------------------------------------
builder.Services.AddDbContext<BioPipelineDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// --- Background pipeline execution ------------------------------------------
// Singleton queue + a single BackgroundService consumer (see
// Services/PipelineRunnerService.cs) -- runs are processed one at a time.
builder.Services.AddSingleton<PipelineRunQueue>();
builder.Services.AddHostedService<PipelineRunnerService>();

// --- Stretch goal: AI explanation (Gemini) -------------------------------
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<GeminiExplanationService>();

// --- API docs -----------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS: allow the Vite dev server to call this API ------------------
const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Create the SQLite file/schema and the run-storage folder on startup.
// EnsureCreated (not migrations) is a deliberate simplification for this
// learning project -- fine for a single-developer SQLite database that gets
// recreated freely; a real project would use `dotnet ef migrations`.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BioPipelineDbContext>();
    db.Database.EnsureCreated();

    var pipelineOptions = scope.ServiceProvider.GetRequiredService<IOptions<PipelineOptions>>().Value;
    Directory.CreateDirectory(pipelineOptions.RunsStorageDir);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(FrontendCorsPolicy);

app.MapRunEndpoints();

app.Run();
