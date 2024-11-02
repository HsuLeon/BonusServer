

using BonusServer;
using BonusServer.Services;
using FunLobbyUtils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net.WebSockets;
using System.Text;

const string confPath = "C:/SignalR/BonusServer";
const string confFile = confPath + "/config.bin";
const string recordsFile = confPath + "/BonusRecords.json";

Log.Path = string.Format("{0}/Log", confPath);
ConfigSetting configSetting = new ConfigSetting(confFile, false);
DBAgent.InitDB(configSetting.DBHost, configSetting.DBPort, false);
BonusAgent.LaunchTime = DateTime.Now;
BonusAgent.WebSite = configSetting.WebSite;
BonusAgent.BonusServerDomain = configSetting.BonusServerDomain;
BonusAgent.BonusServerPort = configSetting.BonusServerPort;
BonusAgent.UpperDomain = configSetting.UpperDomain;
BonusAgent.APITransferPoints = configSetting.APITransferPoints;
BonusAgent.CollectSubScale = configSetting.CollectSubScale;
BonusAgent.InitQueues(configSetting.RabbitMQServer, configSetting.RabbitMQUserName, configSetting.RabbitMQPassword);
BonusAgent.InitTriggers(configSetting.BetWinRule, configSetting.ConditionWinA, configSetting.ConditionWinB, configSetting.ConditionWinCR);
BonusAgent.RestoreRecords(recordsFile);
BonusAgent.StartHeartbeat();

async Task Echo(HttpContext context, WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    while (!result.CloseStatus.HasValue)
    {
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Received: {message}");

        var serverMsg = Encoding.UTF8.GetBytes("Hello from server!");
        await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDev",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                return true;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });

    options.AddPolicy("AllowProd",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost",
                "http://localhost:3000",
                "http://localhost:5000"
            ) // Replace with your front-end origin
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bonus Server", Version = "v1" });

    // 定義 Bearer 身份驗證的安全定義
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // 添加操作篩選器，自動在每個 API 操作上添加 Bearer 身份驗證
    c.OperationFilter<BonusServer.SwaggerBearerAuthOperationFilter>();
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowDev");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

List<WebSocket> list = new List<WebSocket>();

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await Echo(context, webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.Run();
