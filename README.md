# FindAndExploreApi
Azure Serverless Functions Api connecting to MongoDB Atlas on Azure.

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
