using License_Plate_API.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.ToString());
});

var detectModel = new Yolov5DetectModel();
var ocrModel = new Yolov5OCRModel();

builder.Services.AddSingleton(detectModel);
builder.Services.AddSingleton(ocrModel);

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
   app.UseSwagger();
   app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
