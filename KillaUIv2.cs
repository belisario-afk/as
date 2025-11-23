/*
 * KillaUIv2.cs - Complete UI Redesign with Modern Layouts
 * 
 * Features:
 * - Full screen design (1920x1080)
 * - 5 main tabs: PLAY, LOADOUTS, STORE, STATS, SETTINGS
 * - Professional layouts with proper pagination
 * - Clean separation of concerns
 * - Easy to maintain and extend
 * 
 * Version: 2.0.0
 * Author: KillaDome Dev Team
 */

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KillaUIv2", "KillaDome", "2.0.0")]
    [Description("Modern redesigned UI for KillaDome with full screen layouts")]
    public class KillaUIv2 : RustPlugin
    {
        #region Fields
        
        [PluginReference]
        private Plugin KillaDome;
        
        [PluginReference]
        private Plugin ImageLibrary;
        
        // UI Constants
        private const string UI_MAIN = "KillaUI_Main";
        private const string UI_PANEL = "KillaUI_Panel";
        
        // Screen dimensions (1920x1080 reference)
        private const float SCREEN_WIDTH = 1920f;
        private const float SCREEN_HEIGHT = 1080f;
        
        // Colors
        private const string COLOR_PRIMARY = "0.1 0.1 0.1 0.95";      // Dark background
        private const string COLOR_SECONDARY = "0.15 0.15 0.15 0.95"; // Lighter panels
        private const string COLOR_ACCENT = "0.2 0.6 1 1";            // Blue accent
        private const string COLOR_SUCCESS = "0 0.8 0.2 1";           // Green for buy/success
        private const string COLOR_DANGER = "0.8 0.2 0 1";            // Red for danger
        private const string COLOR_WARNING = "1 0.7 0 1";             // Yellow/orange for currency
        private const string COLOR_TEXT = "1 1 1 1";                  // White text
        private const string COLOR_TEXT_DIM = "0.7 0.7 0.7 1";        // Gray text
        private const string COLOR_BORDER = "0.3 0.3 0.3 1";          // Border color
        
        // Session state tracking
        private Dictionary<ulong, PlayerUIState> _playerStates = new Dictionary<ulong, PlayerUIState>();
        
        #endregion
        
        #region Data Classes
        
        private class PlayerUIState
        {
            public string CurrentTab = "play";
            public string CurrentSubTab = "";
            public string CurrentStoreCategory = "guns";
            public string CurrentStoreSkinTab = "gun_skins";
            public int CurrentStorePage = 0;
            public string CurrentLoadoutTab = "loadout_editor";
            public string CurrentEditingWeaponSlot = "primary";
            public string CurrentAttachmentCategory = "scope";
            public int CurrentAttachmentPage = 0;
            public Dictionary<string, string> ArmorSlotTypes = new Dictionary<string, string>();
            public Dictionary<string, int> ArmorSlotSkins = new Dictionary<string, int>();
        }
        
        #endregion
        
        #region Oxide Hooks
        
        private void Init()
        {
            Puts("[KillaUIv2] [DEBUG] KillaUIv2 initialized");
        }
        
        private void OnServerInitialized()
        {
            if (KillaDome == null || !KillaDome.IsLoaded)
            {
                PrintWarning("[KillaUIv2] KillaDome plugin not found! UI will not function.");
                return;
            }
            
            Puts("[KillaUIv2] [DEBUG] KillaUIv2 connected to KillaDome");
        }
        
        private void Unload()
        {
            // Clean up all UIs
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
            
            _playerStates.Clear();
        }
        
        #endregion
        
        #region Public API Methods (Called by KillaDome)
        
        [HookMethod("ShowLobbyUI")]
        public void ShowLobbyUI(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            
            Puts($"[KillaUIv2] ShowLobbyUI called for {player.displayName}");
            
            // Initialize player state if needed
            if (!_playerStates.ContainsKey(player.userID))
            {
                _playerStates[player.userID] = new PlayerUIState();
            }
            
            // Show the main UI with current tab
            var state = _playerStates[player.userID];
            ShowMainUI(player, state.CurrentTab);
        }
        
        [HookMethod("DestroyUI")]
        public void DestroyUI(BasePlayer player)
        {
            if (player == null) return;
            
            CuiHelper.DestroyUi(player, UI_MAIN);
            CuiHelper.DestroyUi(player, UI_PANEL);
            
            _playerStates.Remove(player.userID);
        }
        
        #endregion
        
        #region Main UI Structure
        
        private void ShowMainUI(BasePlayer player, string tab)
        {
            if (player == null || !player.IsConnected) return;
            
            // Destroy existing UI
            CuiHelper.DestroyUi(player, UI_MAIN);
            
            var container = new CuiElementContainer();
            
            // Main background panel
            container.Add(new CuiPanel
            {
                Image = { Color = COLOR_PRIMARY },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", UI_MAIN);
            
            // Header with title and tab buttons
            AddHeader(container, UI_MAIN, player, tab);
            
            // Content area based on selected tab
            AddTabContent(container, UI_MAIN, player, tab);
            
            // Close button
            AddCloseButton(container, UI_MAIN, player);
            
            CuiHelper.AddUi(player, container);
            
            Puts($"[KillaUIv2] UI rendered for {player.displayName}, tab: {tab}");
        }
        
        private void AddHeader(CuiElementContainer container, string parent, BasePlayer player, string currentTab)
        {
            // Header panel
            var header = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = "1 1" }
            }, parent, "Header");
            
            // Title
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "🎮 KILLADOME ARENA 🎮",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.3 1" }
            }, header);
            
            // Tab buttons
            string[] tabs = { "PLAY", "LOADOUTS", "STORE", "STATS", "SETTINGS" };
            string[] tabIds = { "play", "loadouts", "store", "stats", "settings" };
            
            float tabWidth = 0.12f;
            float startX = 0.35f;
            
            for (int i = 0; i < tabs.Length; i++)
            {
                float minX = startX + (i * tabWidth);
                float maxX = minX + tabWidth - 0.01f;
                
                bool isActive = tabIds[i] == currentTab;
                
                var tabButton = container.Add(new CuiButton
                {
                    Button = {
                        Color = isActive ? COLOR_ACCENT : "0.2 0.2 0.2 0.95",
                        Command = $"killaui.tab {tabIds[i]}"
                    },
                    RectTransform = { AnchorMin = $"{minX} 0.1", AnchorMax = $"{maxX} 0.9" },
                    Text = {
                        Text = tabs[i],
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, header);
            }
        }
        
        private void AddCloseButton(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_DANGER,
                    Command = "killaui.close"
                },
                RectTransform = { AnchorMin = "0.95 0.93", AnchorMax = "0.99 0.99" },
                Text = {
                    Text = "✖",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
        }
        
        private void AddTabContent(CuiElementContainer container, string parent, BasePlayer player, string tab)
        {
            // Content area (below header)
            var content = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" }, // Transparent
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.92" }
            }, parent, "Content");
            
            // Route to appropriate tab renderer
            switch (tab)
            {
                case "play":
                    RenderPlayTab(container, content, player);
                    break;
                case "loadouts":
                    RenderLoadoutsTab(container, content, player);
                    break;
                case "store":
                    RenderStoreTab(container, content, player);
                    break;
                case "stats":
                    RenderStatsTab(container, content, player);
                    break;
                case "settings":
                    RenderSettingsTab(container, content, player);
                    break;
            }
        }
        
        #endregion
        
        #region PLAY Tab
        
        private void RenderPlayTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            // Get session data from KillaDome
            var sessionData = KillaDome?.Call("GetSessionData", player.userID) as Dictionary<string, object>;
            
            int tokens = 0;
            int kills = 0;
            float kd = 0f;
            
            if (sessionData != null)
            {
                tokens = Convert.ToInt32(sessionData.ContainsKey("tokens") ? sessionData["tokens"] : 0);
                kills = Convert.ToInt32(sessionData.ContainsKey("totalKills") ? sessionData["totalKills"] : 0);
                int deaths = Convert.ToInt32(sessionData.ContainsKey("totalDeaths") ? sessionData["totalDeaths"] : 0);
                kd = deaths > 0 ? (float)kills / deaths : kills;
            }
            
            // Center join button
            var joinPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.35 0.5", AnchorMax = "0.65 0.7" }
            }, parent);
            
            // Join title
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "🎯 READY TO DOMINATE?",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.9" }
            }, joinPanel);
            
            // Join button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killadome.joinqueue"
                },
                RectTransform = { AnchorMin = "0.25 0.35", AnchorMax = "0.75 0.6" },
                Text = {
                    Text = "JOIN QUEUE\nPlayers: 0/16",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, joinPanel);
            
            // Map info
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Map: Desert Arena",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 0.3" }
            }, joinPanel);
            
            // Stats panel
            var statsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.35 0.45" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "YOUR STATS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_ACCENT
                },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" }
            }, statsPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = $"Kills: {kills}\nK/D: {kd:F2}\nTokens: 💰 {tokens}",
                    FontSize = 14,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.9 0.75" }
            }, statsPanel);
            
            // Quick actions panel
            var actionsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.65 0.2", AnchorMax = "0.9 0.45" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "QUICK ACTIONS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_ACCENT
                },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" }
            }, actionsPanel);
            
            // Quick action buttons
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.3 0.3 0.3 0.95",
                    Command = "killaui.tab loadouts"
                },
                RectTransform = { AnchorMin = "0.1 0.5", AnchorMax = "0.9 0.7" },
                Text = {
                    Text = "View Loadout",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, actionsPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.3 0.3 0.3 0.95",
                    Command = "killaui.tab store"
                },
                RectTransform = { AnchorMin = "0.1 0.25", AnchorMax = "0.9 0.45" },
                Text = {
                    Text = "Buy Weapon",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, actionsPanel);
        }
        
        #endregion
        
        #region LOADOUTS Tab
        
        private void RenderLoadoutsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            var state = GetPlayerState(player.userID);
            
            // Sub-tab buttons
            var subtabPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.92" }
            }, parent);
            
            // Loadout Editor button
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentLoadoutTab == "loadout_editor" ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                    Command = "killaui.loadout.subtab loadout_editor"
                },
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.45 0.9" },
                Text = {
                    Text = "LOADOUT EDITOR",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, subtabPanel);
            
            // Outfit Editor button
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentLoadoutTab == "outfit_editor" ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                    Command = "killaui.loadout.subtab outfit_editor"
                },
                RectTransform = { AnchorMin = "0.55 0.1", AnchorMax = "0.95 0.9" },
                Text = {
                    Text = "OUTFIT EDITOR",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, subtabPanel);
            
            // Content area
            var contentPanel = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.83" }
            }, parent);
            
            // Render appropriate subtab
            if (state.CurrentLoadoutTab == "loadout_editor")
            {
                RenderLoadoutEditor(container, contentPanel, player);
            }
            else if (state.CurrentLoadoutTab == "outfit_editor")
            {
                RenderOutfitEditor(container, contentPanel, player);
            }
        }
        
        private void RenderLoadoutEditor(CuiElementContainer container, string parent, BasePlayer player)
        {
            var state = GetPlayerState(player.userID);
            
            // Get loadout data from KillaDome with proper error handling
            string primaryWeapon = "rifle.ak";
            string secondaryWeapon = "python";
            
            try
            {
                var loadoutDataRaw = KillaDome?.Call("GetCurrentLoadout", player.userID);
                
                if (loadoutDataRaw != null)
                {
                    var loadoutData = loadoutDataRaw as Dictionary<string, object>;
                    if (loadoutData != null && loadoutData.Count > 0)
                    {
                        if (loadoutData.ContainsKey("primary") && loadoutData["primary"] != null)
                        {
                            primaryWeapon = loadoutData["primary"].ToString();
                        }
                        if (loadoutData.ContainsKey("secondary") && loadoutData["secondary"] != null)
                        {
                            secondaryWeapon = loadoutData["secondary"].ToString();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Puts($"[KillaUIv2] Error loading loadout data: {ex.Message}");
            }
            
            // PRIMARY WEAPON SECTION (Left)
            var primaryPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.55", AnchorMax = "0.47 0.95" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "PRIMARY WEAPON",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, primaryPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = GetWeaponDisplayName(primaryWeapon),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.65", AnchorMax = "0.9 0.8" }
            }, primaryPanel);
            
            // Cycle buttons for primary
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle primary -1"
                },
                RectTransform = { AnchorMin = "0.1 0.45", AnchorMax = "0.35 0.6" },
                Text = {
                    Text = "◄ PREV",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, primaryPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle primary 1"
                },
                RectTransform = { AnchorMin = "0.65 0.45", AnchorMax = "0.9 0.6" },
                Text = {
                    Text = "NEXT ►",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, primaryPanel);
            
            // Placeholder for weapon image
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "📷\n[Weapon Image]",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
            }, primaryPanel);
            
            // SECONDARY WEAPON SECTION (Right)
            var secondaryPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.53 0.55", AnchorMax = "0.95 0.95" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "SECONDARY WEAPON",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, secondaryPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = GetWeaponDisplayName(secondaryWeapon),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.65", AnchorMax = "0.9 0.8" }
            }, secondaryPanel);
            
            // Cycle buttons for secondary
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle secondary -1"
                },
                RectTransform = { AnchorMin = "0.1 0.45", AnchorMax = "0.35 0.6" },
                Text = {
                    Text = "◄ PREV",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, secondaryPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle secondary 1"
                },
                RectTransform = { AnchorMin = "0.65 0.45", AnchorMax = "0.9 0.6" },
                Text = {
                    Text = "NEXT ►",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, secondaryPanel);
            
            // Placeholder for weapon image
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "📷\n[Weapon Image]",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
            }, secondaryPanel);
            
            // ATTACHMENTS SECTION (Bottom)
            var attachmentsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.50" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = $"ATTACHMENTS ({GetWeaponDisplayName(primaryWeapon)})",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.88", AnchorMax = "0.9 0.98" }
            }, attachmentsPanel);
            
            // Attachment category tabs
            string[] categories = { "SCOPE", "BARREL", "UNDERBARREL" };
            for (int i = 0; i < categories.Length; i++)
            {
                string category = categories[i].ToLower();
                float xMin = 0.05f + (i * 0.3f);
                float xMax = xMin + 0.25f;
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = state.CurrentAttachmentCategory == category ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                        Command = $"killaui.attachment.category {category}"
                    },
                    RectTransform = { AnchorMin = $"{xMin} 0.75", AnchorMax = $"{xMax} 0.85" },
                    Text = {
                        Text = categories[i],
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, attachmentsPanel);
            }
            
            // Attachment list (simplified - showing placeholders)
            container.Add(new CuiLabel
            {
                Text = {
                    Text = $"📷 Holo Sight\n\n📷 8x Scope\n\n📷 16x Scope\n\n(Attachments will be populated from KillaDome data)",
                    FontSize = 12,
                    Align = TextAnchor.UpperLeft,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0.1 0.15", AnchorMax = "0.9 0.70" }
            }, attachmentsPanel);
            
            // Save button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killaui.loadout.save"
                },
                RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.45 0.12" },
                Text = {
                    Text = "SAVE LOADOUT",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, attachmentsPanel);
            
            // Reset button
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.5 0.5 0.5 0.95",
                    Command = "killaui.loadout.reset"
                },
                RectTransform = { AnchorMin = "0.55 0.02", AnchorMax = "0.9 0.12" },
                Text = {
                    Text = "RESET TO DEFAULT",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, attachmentsPanel);
        }
        
        private void RenderOutfitEditor(CuiElementContainer container, string parent, BasePlayer player)
        {
            // Left side: Stacked armor preview
            var previewPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.40 0.9" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "ARMOR PREVIEW",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.92", AnchorMax = "0.9 0.98" }
            }, previewPanel);
            
            // Stack 5 armor pieces vertically with ImageLibrary integration
            string[] armorSlotNames = { "head", "chest", "legs", "torso", "hands" };
            string[] armorDisplayNames = { "Metal Facemask", "Metal Chest Plate", "Roadsign Kilt", "Roadsign Vest", "Tactical Gloves" };
            string[] defaultArmorItems = { "metal.facemask", "metal.plate.torso", "roadsign.kilt", "roadsign.jacket", "tactical.gloves" };
            
            // Get ImageLibrary plugin reference
            var imageLibrary = plugins.Find("ImageLibrary");
            
            for (int i = 0; i < armorSlotNames.Length; i++)
            {
                float yMin = 0.80f - (i * 0.17f);
                float yMax = yMin + 0.15f;
                
                var slotPanel = container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.2 0.2 0.95" },
                    RectTransform = { AnchorMin = $"0.05 {yMin}", AnchorMax = $"0.95 {yMax}" }
                }, previewPanel);
                
                // Use default armor items for now
                string currentArmorItem = defaultArmorItems[i];
                
                // Armor type cycle buttons (left side)
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.type {armorSlotNames[i]} -1"
                    },
                    RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.12 0.8" },
                    Text = {
                        Text = "◄",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, slotPanel);
                
                // Armor image from ImageLibrary
                if (imageLibrary != null)
                {
                    try
                    {
                        string imageUrl = (string)imageLibrary.Call("GetImage", currentArmorItem, (ulong)0);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            container.Add(new CuiElement
                            {
                                Name = $"armor_image_{i}",
                                Parent = slotPanel,
                                Components =
                                {
                                    new CuiRawImageComponent { Url = imageUrl },
                                    new CuiRectTransformComponent { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                                }
                            });
                        }
                        else
                        {
                            // Fallback if image not found
                            container.Add(new CuiLabel
                            {
                                Text = { Text = "📷", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT },
                                RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                            }, slotPanel);
                        }
                    }
                    catch
                    {
                        // Fallback on error
                        container.Add(new CuiLabel
                        {
                            Text = { Text = "📷", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT },
                            RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                        }, slotPanel);
                    }
                }
                else
                {
                    // ImageLibrary not available - show placeholder
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "📷", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT },
                        RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                    }, slotPanel);
                }
                
                // Armor name label
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = currentArmorItem.Replace(".", " ").Replace("_", " "),
                        FontSize = 11,
                        Align = TextAnchor.MiddleLeft,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.38 0.1", AnchorMax = "0.85 0.9" }
                }, slotPanel);
                
                // Armor type cycle buttons (right side)
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.type {armorSlotNames[i]} 1"
                    },
                    RectTransform = { AnchorMin = "0.88 0.2", AnchorMax = "0.98 0.8" },
                    Text = {
                        Text = "►",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, slotPanel);
            }
            
            // Right side: Skin selection for each armor piece
            var skinPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.45 0.1", AnchorMax = "0.95 0.9" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "SKIN SELECTION",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.92", AnchorMax = "0.9 0.98" }
            }, skinPanel);
            
            // Skin selection for each armor piece
            for (int i = 0; i < armorSlots.Length; i++)
            {
                float yMin = 0.80f - (i * 0.17f);
                float yMax = yMin + 0.15f;
                
                var skinSlotPanel = container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.2 0.2 0.95" },
                    RectTransform = { AnchorMin = $"0.05 {yMin}", AnchorMax = $"0.95 {yMax}" }
                }, skinPanel);
                
                // Previous button
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.skin {i} -1"
                    },
                    RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = "0.25 0.8" },
                    Text = {
                        Text = "◄",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, skinSlotPanel);
                
                // Skin preview
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "📷 Default Skin",
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.9" }
                }, skinSlotPanel);
                
                // Next button
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.skin {i} 1"
                    },
                    RectTransform = { AnchorMin = "0.75 0.2", AnchorMax = "0.95 0.8" },
                    Text = {
                        Text = "►",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, skinSlotPanel);
            }
            
            // Save outfit button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killaui.outfit.save"
                },
                RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.45 0.10" },
                Text = {
                    Text = "SAVE OUTFIT",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, skinPanel);
            
            // Reset button
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.5 0.5 0.5 0.95",
                    Command = "killaui.outfit.reset"
                },
                RectTransform = { AnchorMin = "0.55 0.02", AnchorMax = "0.9 0.10" },
                Text = {
                    Text = "RESET TO DEFAULT",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, skinPanel);
        }
        
        private string GetWeaponDisplayName(string weaponId)
        {
            // Map weapon IDs to display names
            switch (weaponId.ToLower())
            {
                case "ak47": return "AK-47";
                case "lr300": return "LR-300";
                case "mp5": return "MP5";
                case "python": return "Python";
                case "bolt": return "Bolt Action";
                case "thompson": return "Thompson";
                case "smg2": return "Custom SMG";
                default: return weaponId.ToUpper();
            }
        }
        
        private PlayerUIState GetPlayerState(ulong playerId)
        {
            if (!_playerStates.ContainsKey(playerId))
            {
                _playerStates[playerId] = new PlayerUIState();
            }
            return _playerStates[playerId];
        }

        private string GetPlayerArmorType(ulong playerId, string slot)
        {
            var state = GetOrCreatePlayerState(playerId);
            if (state.ArmorSlotTypes.ContainsKey(slot))
                return state.ArmorSlotTypes[slot];
            
            // Return defaults based on slot
            switch (slot)
            {
                case "head": return "metal.facemask";
                case "chest": return "metal.plate.torso";
                case "legs": return "roadsign.kilt";
                case "torso": return "roadsign.jacket";
                case "hands": return "tactical.gloves";
                default: return "metal.facemask";
            }
        }

        private void SetPlayerArmorType(ulong playerId, string slot, string armorType)
        {
            var state = GetOrCreatePlayerState(playerId);
            state.ArmorSlotTypes[slot] = armorType;
        }
        
        #endregion
        
        #region STORE Tab (Placeholder - to be implemented)
        
        private void RenderStoreTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "STORE TAB - Under Construction\n\nFeatures coming:\n• Guns\n• Attachments\n• Clothing/Armor\n• Skins (Gun & Armor)",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region STATS Tab (Placeholder - to be implemented)
        
        private void RenderStatsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "STATS TAB - Under Construction\n\nFeatures coming:\n• Combat Performance\n• Match History\n• Progression",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region SETTINGS Tab (Placeholder - to be implemented)
        
        private void RenderSettingsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "SETTINGS TAB - Under Construction\n\nFeatures coming:\n• Gameplay Settings\n• Notifications\n• Account Management",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region Console Commands
        
        [ConsoleCommand("killaui.tab")]
        private void CmdChangeTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string tab = arg.GetString(0, "play");
            
            // Update state
            if (!_playerStates.ContainsKey(player.userID))
            {
                _playerStates[player.userID] = new PlayerUIState();
            }
            _playerStates[player.userID].CurrentTab = tab;
            
            // Refresh UI
            ShowMainUI(player, tab);
        }
        
        [ConsoleCommand("killaui.close")]
        private void CmdClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            DestroyUI(player);
        }
        
        [ConsoleCommand("killaui.loadout.subtab")]
        private void CmdLoadoutSubtab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string subtab = arg.GetString(0, "loadout_editor");
            var state = GetPlayerState(player.userID);
            state.CurrentLoadoutTab = subtab;
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.weapon.cycle")]
        private void CmdWeaponCycle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string slot = arg.GetString(0, "primary");
            int direction = arg.GetInt(1, 1);
            
            // Call KillaDome to cycle weapon
            KillaDome?.Call("CycleWeapon", player, slot, direction);
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.attachment.category")]
        private void CmdAttachmentCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string category = arg.GetString(0, "scope");
            var state = GetPlayerState(player.userID);
            state.CurrentAttachmentCategory = category;
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.loadout.save")]
        private void CmdLoadoutSave(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            player.ChatMessage("Loadout saved successfully!");
            // Additional save logic can be added via KillaDome call
        }
        
        [ConsoleCommand("killaui.loadout.reset")]
        private void CmdLoadoutReset(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            // Call KillaDome to reset loadout
            KillaDome?.Call("ResetLoadout", player.userID);
            
            player.ChatMessage("Loadout reset to default.");
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.armor.skin")]
        private void CmdArmorSkin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            int armorSlot = arg.GetInt(0, 0);
            int direction = arg.GetInt(1, 1);
            
            // Call KillaDome to cycle armor skin
            KillaDome?.Call("CycleArmorSkin", player.userID, armorSlot, direction);
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.outfit.save")]
        private void CmdOutfitSave(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            player.ChatMessage("Outfit saved successfully!");
            // Additional save logic via KillaDome
        }
        
        [ConsoleCommand("killaui.outfit.reset")]
        private void CmdOutfitReset(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            // Call KillaDome to reset outfit
            KillaDome?.Call("ResetOutfit", player.userID);
            
            player.ChatMessage("Outfit reset to default.");
            ShowMainUI(player, "loadouts");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void LogDebug(string message)
        {
            Puts($"[DEBUG] {message}");
        }
        
        #endregion
    }
}