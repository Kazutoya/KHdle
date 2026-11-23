# KHdle

Jeu quotidien façon Wordle/mcdle sur l'univers Kingdom Hearts, en Blazor Server (.NET 10).


## Modes

Personnage, Boss, Monde, Keyblade, Ennemi. Site bilingue FR/EN (sélecteur en haut à droite). Une réponse par mode et par jour (minuit, heure de Paris),
calculée côté serveur à partir de la date : tout le monde a la même énigme.

## Modifier ou ajouter des données

Chaque mode est un fichier JSON dans `Data/`. 
Types de colonne : `text` (identique ou non), `list` (valeurs séparées par `|`, correspondance partielle possible),
`number` (flèche plus haut / plus bas).


## Structure

- `Data/*.json` : contenu des modes
- `Services/GameService.cs` : chargement, réponse du jour, comparaison des indices, recherche
- `Services/ImageService.cs` : gestion des images correspondantes aux entrées
- `Components/Pages/Play.razor` : page de jeu (progression sauvegardée dans le navigateur)
- `wwwroot/app.css` : thème
