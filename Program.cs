using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        // C'est ici que tu ajouteras tes services si nécessaire plus tard
    })
    .Build();

host.Run();