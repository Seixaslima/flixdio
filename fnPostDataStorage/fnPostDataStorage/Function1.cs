using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;

namespace fnPostDataStorage
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("dataStorage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processando a imagem no storage");
            try
            {
                if (!req.Headers.TryGetValue("file-type", out var fileTypeHeaders))
                {
                    return new BadRequestObjectResult("O cabeçalho 'file-type' é obrigatório");
                }
                var filetype = fileTypeHeaders.ToString();
                var form = await req.ReadFormAsync();
                var file = form.Files["file"];

                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult("O arquivo não foi enviado");
                }

                string conectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = filetype;

                BlobClient blobClient = new BlobClient(conectionString, containerName, file.FileName);
                BlobContainerClient containerClient = new BlobContainerClient(conectionString, containerName);

                await blobClient.UploadAsync(file.OpenReadStream(), true);

                await containerClient.CreateIfNotExistsAsync();
                await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

                string blobName = file.FileName;
                var blob = containerClient.GetBlobClient(blobName);

                using (var stream = file.OpenReadStream())
                {
                    await blob.UploadAsync(stream, true);
                }

                _logger.LogInformation($"Imagem {file.FileName} processada com sucesso");

                return new OkObjectResult(new
                {
                    message = "Imagem processada com sucesso",
                    url = blob.Uri
                });

            }
            catch
            { return new BadRequestObjectResult("Erro ao processar a imagem"); }
        }
    }
}
