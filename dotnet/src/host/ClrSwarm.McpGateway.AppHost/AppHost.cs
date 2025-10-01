var builder = DistributedApplication.CreateBuilder(args);


var redis = builder.AddRedis("redis");

var mongo = builder.AddMongoDB("mongo").WithMongoExpress()
                 .WithDataVolume("demo");

var mcpGatewayDb = mongo.AddDatabase("mcpgateway");

var mcpGatewayService = builder.AddProject<Projects.ClrSwarm_McpGateway_Service>("mcpgateway-service")
        .WithReference(mcpGatewayDb).WaitFor(mcpGatewayDb)
        .WithReference(redis).WaitFor(redis);

builder.Build().Run();
