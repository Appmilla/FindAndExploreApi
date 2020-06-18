# FindAndExploreApi
Azure Serverless Functions Api connecting to MongoDB Atlas on Azure.

Login to MongoDB at https://account.mongodb.com/account/login using rich work email and Incywincy100

click on Collections and you can view the data

click on the Connect button and choose the bottom option 'Connect using MongoDB compass. this will give you the connection string and download libks for the Compass app which is good for viewing and modifying the database

Queries for Postman

To the app service:

https://findandexploreapi-dev.azurewebsites.net/api/GetCurrentArea?lat=51.0664995383346f&lon=-3.0453250843303f
https://findandexploreapi-dev.azurewebsites.net/api/GetAreasPointsOfInterest?locationId=1234

Using Api Management:

Add these two header key-values:-
Ocp-Apim-Subscription-Key 12e7dd82e8e94a67a4cc70663f9cf46d
Ocp-Apim-Trace true

https://apim-find-and-explore.azure-api.net/FindAndExploreApi-dev/GetCurrentArea?lat=51.0664995383346f&lon=-3.0453250843303f

https://apim-find-and-explore.azure-api.net/FindAndExploreApi-dev/GetAreasPointsOfInterest?locationId=1234


The Api contract models are contained in the nuget package.

to publish the nuget package to Azure DevOps:-

Create an xml file called nuget.config containing:-

<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="appmilla" value="https://pkgs.dev.azure.com/appmilla/_packaging/appmilla/nuget/v3/index.json" />
  </packageSources>
</configuration>


This line below works when in the directory C:\GitHub\FindAndExploreApi\FindAndExploreApi.Client>
C:\Nuget\nuget.exe push -Source "appmilla" -ApiKey az C:\GitHub\FindAndExploreApi\FindAndExploreApi.Client\bin\Debug\FindAndExploreApi.Client.1.0.0.nupkg
