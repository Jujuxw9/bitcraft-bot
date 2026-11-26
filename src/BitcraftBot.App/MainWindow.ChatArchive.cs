using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Archive du chat en local (v0.4.16, D-068) : chaque message du chat vu par le bot (Local, Région, LFG, Trade)
// et les messages privés lus pendant la découverte (module global) vont dans une base SQLite
// « archive-chat\chat.db » à côté du .exe. L'écriture se fait sur un fil à part : la fenêtre n'attend jamais.
// Onglet CHAT : tableau des messages archivés (v0.4.20 : le plus récent en bas, comme le journal, mis à jour chaque seconde ;
// les nouveaux messages s'ajoutent en bas et s'éclairent un instant), recherche par joueur, filtre par canal.
public partial class MainWindow
{
    private const int ChatArchiveShown = 500;

    // Un message de Julien attend 10 s avant d'être archivé : le temps de savoir si c'est une réponse du bot (@gpt).
    private static readonly TimeSpan OwnMessageDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ChatArchiveRefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ChatNewHighlight = TimeSpan.FromSeconds(4);
    private readonly System.Collections.ObjectModel.ObservableCollection<ChatArchiveRow> _chatArchiveRows = [];
    private string? _chatArchiveFilterKey;

    private ChatArchive? _chatArchive;
    private readonly HashSet<string> _chatArchiveSeen = [];
    private readonly Channel<IReadOnlyList<ArchivedChatMessage>> _chatArchiveQueue =
        Channel.CreateUnbounded<IReadOnlyList<ArchivedChatMessage>>(new UnboundedChannelOptions { SingleReader = true });
    private RegionDiscovery? _chatArchiveDmsFrom;
    private long _chatArchiveCount;
    private int _chatArchiveAddedThisRun;
    private volatile bool _chatArchiveDirty = true;
    private DateTime _chatArchiveRefreshedAt = DateTime.MinValue;
    private bool _chatArchiveRefreshing;

    // Ligne du tableau. IsNew : message arrivé depuis le dernier affichage (fond éclairé quelques secondes).
    public sealed class ChatArchiveRow(string time, string channel, string player, string text, string tip) : System.ComponentModel.INotifyPropertyChanged
    {
        public string Time { get; } = time;
        public string Channel { get; } = channel;
        public string Player { get; } = player;
        public string Text { get; } = text;
        public string Tip { get; } = tip;
        public string Key => string.Join('\u001f', Time, Channel, Player, Text);

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set
            {
                if (_isNew != value)
                {
                    _isNew = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsNew)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private void InitChatArchive()
    {
        ChatArchiveChannelBox.ItemsSource = new[] { "Tous les canaux", "Local", "Région", "Looking For Group", "Trade", "Privé" };
        ChatArchiveChannelBox.SelectedIndex = 0;
        var folder = Path.Combine(AppContext.BaseDirectory, ChatArchive.FolderName);
        _ = Task.Run(() =>
        {
            try
            {
                var archive = ChatArchive.OpenFolder(folder);
                var count = archive.Count();
                Interlocked.Exchange(ref _chatArchiveCount, count);
                _chatArchive = archive;
                _log.Info("Chat", $"Archive du chat : {count} messages, fichier {archive.Location}.");
                _chatArchiveDirty = true;
                _ = Task.Run(WriteChatArchiveAsync);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException or InvalidOperationException)
            {
                _log.Alerte("Chat", $"Archive du chat impossible à ouvrir ({folder}) : {ex.Message}. Le chat ne sera pas archivé.");
            }
        });
    }

    // Appelé toutes les 500 ms avec la lecture du serveur : repère les nouveaux messages et les envoie au fil d'écriture.
    private void ArchiveChat(ServerReader reader, PlayerSnapshot s)
    {
        if (_chatArchive is null)
        {
            return;
        }
        var batch = new List<ArchivedChatMessage>();
        if (reader.Discovery is { } d && !ReferenceEquals(d, _chatArchiveDmsFrom))
        {
            _chatArchiveDmsFrom = d;
            foreach (var dm in d.DirectMessages)
            {
                if (_chatArchiveSeen.Add($"{ChatArchive.KindPrivate}:{dm.EntityId}"))
                {
                    batch.Add(ChatArchive.From(dm));
                }
            }
        }
        if (reader.State == ReaderState.Connecte && s.EntityId is { } me)
        {
            var now = DateTimeOffset.UtcNow;
            var region = string.IsNullOrWhiteSpace(s.RegionName) ? null : s.RegionName;
            foreach (var m in reader.ChatMessages())
            {
                var key = $"{ChatArchive.KindChat}:{m.EntityId}";
                if (_chatArchiveSeen.Contains(key))
                {
                    continue;
                }
                var own = m.OwnerEntityId == me;
                if (own && now - m.SentAt < OwnMessageDelay)
                {
                    continue;
                }
                _chatArchiveSeen.Add(key);
                batch.Add(ChatArchive.From(m, region, own && _gptPicker is { } p && p.WrittenByBot(m)));
            }
        }
        if (batch.Count > 0)
        {
            _chatArchiveQueue.Writer.TryWrite(batch);
        }
        if (Tabs.SelectedItem == ChatTabItem && _chatArchiveDirty && DateTime.Now - _chatArchiveRefreshedAt >= ChatArchiveRefreshInterval)
        {
            RefreshChatArchive();
        }
    }

    // Fil d'écriture : un seul, hors de la fenêtre.
    private async Task WriteChatArchiveAsync()
    {
        await foreach (var batch in _chatArchiveQueue.Reader.ReadAllAsync())
        {
            if (_chatArchive is not { } archive)
            {
                return;
            }
            try
            {
                var added = archive.Add(batch);
                if (added > 0)
                {
                    Interlocked.Add(ref _chatArchiveCount, added);
                    var first = Interlocked.Add(ref _chatArchiveAddedThisRun, added) == added;
                    if (first)
                    {
                        _log.Info("Chat", $"Archive du chat : premiers messages enregistrés ({added}).");
                    }
                    _chatArchiveDirty = true;
                }
            }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or InvalidOperationException or IOException)
            {
                _log.Alerte("Chat", $"Archive du chat : {batch.Count} message(s) pas enregistré(s) : {ex.Message}.");
            }
        }
    }

    // Tableau de l'onglet CHAT : lu sur un fil à part, puis affiché.
    private void RefreshChatArchive()
    {
        if (_chatArchive is not { } archive || _chatArchiveRefreshing)
        {
            return;
        }
        _chatArchiveRefreshing = true;
        _chatArchiveDirty = false;
        _chatArchiveRefreshedAt = DateTime.Now;
        var player = ChatArchivePlayerBox.Text;
        int? channel = ChatArchiveChannelBox.SelectedIndex switch
        {
            1 => (int)ChatChannel.Local,
            2 => (int)ChatChannel.Region,
            3 => (int)ChatChannel.RechercheGroupe,
            4 => (int)ChatChannel.Commerce,
            5 => ChatArchive.PrivateChannel,
            _ => null,
        };
        _ = Task.Run(() =>
        {
            try
            {
                var list = archive.Search(player, channel, ChatArchiveShown);
                var rows = list.Select(m => new ChatArchiveRow(
                    m.SentAt.ToLocalTime().ToString("dd/MM HH:mm:ss"),
                    m.ChannelName,
                    m.PlayerName + (m.ByBot ? " (bot)" : ""),
                    m.Text,
                    m.Text + (m.Region is { } r ? $"\nRégion : {r}" : ""))).ToList();
                // L'archive rend le plus récent d'abord : on retourne la liste pour l'avoir en bas.
                rows.Reverse();
                var filterKey = $"{player.Trim()}\u001f{channel}";
                Dispatcher.BeginInvoke(() =>
                {
                    ShowChatRows(rows, filterKey);
                    var total = Interlocked.Read(ref _chatArchiveCount);
                    var filtered = player.Trim().Length > 0 || channel is not null;
                    ChatArchiveCount.Text = $"{total:N0} messages archivés"
                        + (filtered ? $", {rows.Count:N0}{(rows.Count >= ChatArchiveShown ? "+" : "")} trouvés" : "")
                        + (rows.Count >= ChatArchiveShown ? $" ({ChatArchiveShown} plus récents affichés)" : "");
                    _chatArchiveRefreshing = false;
                });
            }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or InvalidOperationException)
            {
                _log.Alerte("Chat", $"Archive du chat : lecture impossible : {ex.Message}.");
                _chatArchiveRefreshing = false;
            }
        });
    }

    // Met le tableau à jour sans tout refaire : les messages déjà affichés restent, les nouveaux s'ajoutent en bas
    // (éclairés quelques secondes). Si on était tout en bas, on y reste ; sinon on ne bouge pas la liste.
    private void ShowChatRows(List<ChatArchiveRow> rows, string filterKey)
    {
        if (ChatArchiveList.ItemsSource != _chatArchiveRows)
        {
            ChatArchiveList.ItemsSource = _chatArchiveRows;
        }
        var scroller = FindScroller(ChatArchiveList);
        var atBottom = scroller is null || scroller.VerticalOffset >= scroller.ScrollableHeight - 2;
        var lastKey = _chatArchiveRows.Count > 0 ? _chatArchiveRows[^1].Key : null;
        var start = lastKey is null ? -1 : rows.FindLastIndex(r => r.Key == lastKey);
        var added = new List<ChatArchiveRow>();
        if (filterKey != _chatArchiveFilterKey || start < 0)
        {
            // Nouveau filtre (ou trop de nouveaux messages d'un coup) : tableau refait, sans éclairage.
            _chatArchiveFilterKey = filterKey;
            _chatArchiveRows.Clear();
            foreach (var row in rows)
            {
                _chatArchiveRows.Add(row);
            }
            atBottom = true;
        }
        else
        {
            foreach (var row in rows.Skip(start + 1))
            {
                row.IsNew = true;
                _chatArchiveRows.Add(row);
                added.Add(row);
            }
            while (_chatArchiveRows.Count > ChatArchiveShown)
            {
                _chatArchiveRows.RemoveAt(0);
            }
        }
        if (atBottom && _chatArchiveRows.Count > 0)
        {
            ChatArchiveList.ScrollIntoView(_chatArchiveRows[^1]);
        }
        if (added.Count > 0)
        {
            _ = Task.Delay(ChatNewHighlight).ContinueWith(_ => Dispatcher.BeginInvoke(() =>
            {
                foreach (var row in added)
                {
                    row.IsNew = false;
                }
            }));
        }
    }

    private static ScrollViewer? FindScroller(DependencyObject root)
    {
        if (root is ScrollViewer s)
        {
            return s;
        }
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindScroller(System.Windows.Media.VisualTreeHelper.GetChild(root, i)) is { } found)
            {
                return found;
            }
        }
        return null;
    }

    private void ChatArchiveFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_chatArchive is null || ChatArchiveList is null)
        {
            return;
        }
        _chatArchiveRefreshing = false;
        RefreshChatArchive();
    }

    private void Tabs_SelectionChangedForChat(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == Tabs && Tabs.SelectedItem == ChatTabItem)
        {
            _chatArchiveDirty = true;
            RefreshChatArchive();
        }
    }

    private void CloseChatArchive()
    {
        _chatArchiveQueue.Writer.TryComplete();
        var archive = _chatArchive;
        _chatArchive = null;
        archive?.Dispose();
    }
}
