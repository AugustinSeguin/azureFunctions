# Azure Functions - HTTP vers Queue vers Table

Ce dépôt contient une application Azure Functions .NET isolée qui met en place une chaîne simple et stateless : une requête HTTP publie un message dans une Queue Storage, puis une seconde fonction déclenchée par cette queue écrit le résultat dans une Table Storage.

## Architecture

Le flux est volontairement minimal :

1. La fonction HTTP reçoit un `POST` JSON avec `name` et `message`.
2. Elle valide uniquement les champs obligatoires et publie un message dans la queue `incoming-requests`.
3. La fonction Queue Trigger consomme ce message et écrit une entité dans la table `incomingrequests`.

La communication entre les deux fonctions passe uniquement par la queue. Aucune fonction n’en appelle une autre directement.

## Fonctions

### `Producer`

Déclenchement HTTP.

Rôle :

- recevoir la requête HTTP
- extraire les données reçues
- publier un message dans Queue Storage

### `Consumer`

Déclenchement Queue Storage.

Rôle :

- être déclenchée à l’arrivée d’un message
- lire le contenu du message
- écrire les données dans Table Storage

## Configuration locale

Le projet utilise la configuration locale suivante :

- `AzureWebJobsStorage=UseDevelopmentStorage=true`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

Le stockage local est prévu pour Azurite. Le dépôt contient aussi les fichiers générés localement pour la file et la table.

## Lancer le projet en local

1. Démarrer Azurite ou vérifier que `UseDevelopmentStorage=true` pointe bien vers une instance locale disponible.
2. Restaurer et compiler le projet :

```bash
dotnet build
```

3. Lancer l’application Functions :

```bash
func start
```

Si `func` n’est pas disponible, tu peux aussi lancer le projet depuis le dossier racine avec `dotnet run`.

## Vérification de l’enchaînement

1. Envoyer une requête HTTP `POST` vers la fonction `Producer` avec un corps JSON du type :

```json
{
  "name": "Alice",
  "message": "Bonjour"
}
```

2. Vérifier dans les logs que le message a bien été publié dans la queue `incoming-requests`.
3. Vérifier que la fonction `Consumer` a bien été déclenchée par la queue.
4. Contrôler l’écriture dans la table `incomingrequests`.

## Logs attendus

En exécution normale, tu dois voir :

- la réception de la requête HTTP
- la publication du message dans la queue
- le déclenchement de la fonction queue
- l’écriture dans la table storage

## Livrable

Le dépôt contient :

- le code des deux fonctions
- les fichiers de configuration pour l’exécution locale
- ce README avec l’architecture, le rôle des fonctions et la procédure de lancement en local
