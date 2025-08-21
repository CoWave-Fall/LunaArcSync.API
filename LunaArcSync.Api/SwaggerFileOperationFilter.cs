using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParameters = context.ApiDescription.ActionDescriptor.Parameters
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (formFileParameters.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = formFileParameters.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema { Type = "string", Format = "binary" })
                        }
                    }
                }
            };
        }
    }
}