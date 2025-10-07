# Steam Search Browser for Playnite

A Playnite plugin that allows you to search and discover games on Steam directly from within Playnite, then add them to your library as non-installed entries.

## Features

### Steam Search Browser
- **On-Demand Search**: No preloading - the plugin stays empty until you enter a search query
- **Real-time Results**: Search Steam's catalog and get up to 30 matching results with cover art
- **Detailed Information**: View game descriptions, developers, publishers, genres, release dates, and pricing
- **Direct Integration**: Add any game to your Playnite library with a single click
- **Steam Store Links**: Quick access to view games on the Steam store page
- **Fullscreen Compatible**: Works seamlessly in both desktop and fullscreen modes

### Game Pass Catalog Browser (Legacy)
The original Game Pass catalog browsing functionality is still available as a separate option.

## Usage

### Desktop Mode
1. Open Playnite
2. Go to the main menu and select **Steam Search Browser → Search Steam Games**
3. Enter a game name in the search box and press Enter or click "Search"
4. Browse through the results - click on any game to see its details
5. Click "Add to Library" to add the game to your Playnite library
6. Click "View on Steam" to open the game's Steam store page

### Fullscreen Mode
1. Open Playnite in Fullscreen mode
2. Navigate to the sidebar
3. Select **Steam Search** 
4. Use your controller/keyboard to enter a search query
5. Browse and select games to view details
6. Add games to your library or view them on Steam

## How It Works

1. **Search**: When you enter a search query, the plugin queries Steam's public search API
2. **Fetch Details**: For each result, it retrieves comprehensive game information including:
   - Cover art and background images
   - Full description
   - Developer and publisher information
   - Genre and category tags
   - Release date
   - Current pricing
3. **Add to Library**: When you add a game, it creates a new entry in your Playnite library with:
   - All metadata populated
   - Cover and background images downloaded
   - Marked as "not installed"
   - Steam as the source
   - Link to the Steam store page

## Technical Details

### API Endpoints Used
- **Steam Search API**: `https://steamcommunity.com/actions/SearchApps/`
- **Steam App Details API**: `https://store.steampowered.com/api/appdetails`

### Performance
- Search results are limited to 30 games to ensure quick loading
- A small delay between API calls prevents rate limiting
- Images are downloaded and cached in Playnite's database
- All operations are asynchronous to keep the UI responsive

## Benefits Over Traditional Methods

1. **No Browser Required**: Browse and add Steam games without leaving Playnite
2. **Unified Library**: Add games you're interested in to track, organize, and wishlist them
3. **Fast and Lightweight**: Only loads data when you search, no background processes
4. **Clean Integration**: Games are added with proper metadata and artwork
5. **Fullscreen Friendly**: Designed to work seamlessly with controller navigation

## Requirements

- Playnite 6.9.0 or higher
- Internet connection for Steam API access

## Notes

- Games are added as "not installed" entries - you'll need to install them through Steam
- The plugin uses public Steam APIs and does not require Steam to be running
- HTML in game descriptions is automatically stripped and formatted
- If a game already exists in your library, the "Add to Library" button will be disabled

## Future Enhancements

Potential features for future versions:
- Filter by genre, price range, or release year
- Sort results by relevance, popularity, or release date
- Bulk add multiple games at once
- Integration with Steam wishlist
- Support for other platforms (Epic, GOG, etc.)

## Credits

Based on the Game Pass Catalog Browser plugin architecture, adapted for Steam search functionality.
