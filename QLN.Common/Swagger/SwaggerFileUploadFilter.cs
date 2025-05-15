using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Swagger
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType.GetProperties()?
                    .Any(prop => prop.PropertyType == typeof(IFormFile) || prop.PropertyType == typeof(IFormFile[])) == true)
                .ToList();

            if (!fileParams.Any()) return;

            foreach (var param in fileParams)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(param.ParameterType, context.SchemaRepository)
                        }
                    }
                };
            }
        }
    }
}
