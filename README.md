# KHdle

Jeu quotidien façon Wordle/mcdle sur l'univers Kingdom Hearts, en Blazor Server (.NET 8).


## Modes

Personnage, Boss, Monde, Keyblade, Ennemi. Site bilingue FR/EN (sélecteur en haut à droite). Une réponse par mode et par jour (minuit, heure de Paris),
calculée côté serveur à partir de la date : tout le monde a la même énigme.

## Modifier ou ajouter des données

Chaque mode est un fichier JSON dans `Data/`. Pour ajouter une entrée, copie une ligne de `entries`
et renseigne une valeur pour chaque colonne (les nombres s'écrivent entre guillemets).
Une valeur manquante ou un doublon fait échouer le démarrage avec un message explicite.

Types de colonne : `text` (identique ou non), `list` (valeurs séparées par `|`, correspondance partielle possible),
`number` (flèche plus haut / plus bas).

Pour ajouter un mode, crée un nouveau fichier JSON dans `Data/` (champ `order` pour sa position).

## Structure

- `Data/*.json` : contenu des modes
- `Services/GameService.cs` : chargement, réponse du jour, comparaison des indices, recherche
- `Components/Pages/Play.razor` : page de jeu (progression sauvegardée dans le navigateur)
- `wwwroot/app.css` : thème
