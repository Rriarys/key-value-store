using KeyValueStore.Server.Application;

var application = new ConsoleServerApplication();
return await application.RunAsync(args);
