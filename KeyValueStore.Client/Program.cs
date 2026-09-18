using KeyValueStore.Client.Application;
using KeyValueStore.Client.Commands;

var application = new ConsoleClientApplication(new ClientCommandParser());
return await application.RunAsync(args);
