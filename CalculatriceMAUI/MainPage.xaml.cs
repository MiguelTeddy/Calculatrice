using System.Globalization;

namespace CalculatriceMAUI;

public partial class MainPage : ContentPage
{
    // ===== État de la calculatrice =====
    private string _operation = "";
    private string _saisie = "0";
    private double _accumulateur = 0;
    private string _operateur = "";
    private bool _nouvelleSaisie = true;

    // ===== Couleurs centralisées =====
    private static readonly Color CouleurChiffre = Color.FromArgb("#505050");
    private static readonly Color CouleurOperateur = Color.FromArgb("#0A84FF");
    private static readonly Color CouleurFonction = Color.FromArgb("#64B5F6");
    private static readonly Color CouleurHover = Color.FromArgb("#3A3A3C");
    private static readonly Color CouleurPress = Color.FromArgb("#0070E0");

    public MainPage()
    {
        InitializeComponent();
        ConstruireClavier();
        MettreAJourAffichage();
    }

    // =========================================================
    //  CONSTRUCTION DU CLAVIER
    // =========================================================
    private void ConstruireClavier()
    {
        // (texte, type, ligne, colonne)
        var touches = new (string Texte, string Type, int Ligne, int Col)[]
        {
            ("AC",  "fonction",  0, 0), ("+/-", "fonction",  0, 1), ("%",   "fonction",  0, 2), ("/", "operateur", 0, 3),
            ("7",   "chiffre",   1, 0), ("8",   "chiffre",   1, 1), ("9",   "chiffre",   1, 2), ("*", "operateur", 1, 3),
            ("4",   "chiffre",   2, 0), ("5",   "chiffre",   2, 1), ("6",   "chiffre",   2, 2), ("-", "operateur", 2, 3),
            ("1",   "chiffre",   3, 0), ("2",   "chiffre",   3, 1), ("3",   "chiffre",   3, 2), ("+", "operateur", 3, 3),
            ("0",   "chiffre",   4, 0), (".",   "chiffre",   4, 1), ("x^y", "fonction",  4, 2), ("=", "egal",      4, 3),
        };

        foreach (var t in touches)
        {
            var couleurBase = t.Type switch
            {
                "chiffre" => CouleurChiffre,
                "operateur" => CouleurOperateur,
                "fonction" => CouleurFonction,
                "egal" => CouleurOperateur,
                _ => Colors.Gray
            };

            var btn = new Button
            {
                Text = t.Texte,
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = couleurBase,
                CornerRadius = 100,
                Margin = 0,
                Padding = 0,
                MinimumHeightRequest = 50,
                MinimumWidthRequest = 50
            };

            // --- Effet "pressé" (au toucher) via VisualStateManager ---
            var vsm = new VisualStateGroupList();
            var groupe = new VisualStateGroup { Name = "CommonStates" };

            var normal = new VisualState { Name = "Normal" };
            normal.Setters.Add(new Setter
            {
                Property = Button.ScaleProperty,
                Value = 1.0
            });

            var pressed = new VisualState { Name = "Pressed" };
            pressed.Setters.Add(new Setter
            {
                Property = Button.ScaleProperty,
                Value = 0.92
            });
            pressed.Setters.Add(new Setter
            {
                Property = Button.BackgroundColorProperty,
                Value = CouleurPress
            });

            groupe.States.Add(normal);
            groupe.States.Add(pressed);
            vsm.Add(groupe);
            VisualStateManager.SetVisualStateGroups(btn, vsm);

            // --- Effet "hover" (survol souris, Windows/Mac) ---
            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += (s, e) =>
            {
                btn.BackgroundColor = CouleurHover;
                btn.Scale = 1.05;
            };
            pointer.PointerExited += (s, e) =>
            {
                btn.BackgroundColor = couleurBase;
                btn.Scale = 1.0;
            };
            btn.GestureRecognizers.Add(pointer);

            // --- Gestion du clic ---
            btn.Clicked += OnToucheClicked;

            // --- Placement dans la Grid (responsive) ---
            Grid.SetRow(btn, t.Ligne);
            Grid.SetColumn(btn, t.Col);
            ClavierGrid.Children.Add(btn);
        }
    }

    // =========================================================
    //  GESTION DES TOUCHES
    // =========================================================
    private void OnToucheClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        var texte = btn.Text;

        try
        {
            switch (texte)
            {
                case "AC":
                    _operation = ""; _saisie = "0"; _accumulateur = 0;
                    _operateur = ""; _nouvelleSaisie = true;
                    break;

                case "DEL":
                    if (_saisie.Length > 1 && _saisie != "Division par zéro")
                        _saisie = _saisie[..^1];
                    else
                        _saisie = "0";
                    break;

                case "+/-":
                    if (_saisie.StartsWith('-'))
                        _saisie = _saisie[1..];
                    else if (_saisie != "0")
                        _saisie = "-" + _saisie;
                    break;

                case "%":
                    if (double.TryParse(_saisie, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var v))
                        _saisie = (v / 100).ToString(CultureInfo.InvariantCulture);
                    break;

                case ".":
                    if (!_saisie.Contains('.')) _saisie += ".";
                    break;

                case "+":
                case "-":
                case "*":
                case "/":
                    AppliquerOperateur(texte);
                    break;

                case "x^y":
                    AppliquerOperateur("^");
                    break;

                case "=":
                    Calculer();
                    _operation = "";
                    _operateur = "";
                    _nouvelleSaisie = true;
                    break;

                default: // chiffres
                    if (_saisie == "Division par zéro") _saisie = "0";
                    if (_nouvelleSaisie) { _saisie = texte; _nouvelleSaisie = false; }
                    else if (_saisie == "0") _saisie = texte;
                    else _saisie += texte;
                    break;
            }
        }
        catch (Exception ex)
        {
            _saisie = "Erreur";
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }

        MettreAJourAffichage();
    }

    // =========================================================
    //  LOGIQUE DE CALCUL
    // =========================================================
    private void AppliquerOperateur(string op)
    {
        if (double.TryParse(_saisie, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var valeur))
        {
            if (_operateur != "" && !_nouvelleSaisie)
                Calculer();

            if (_operateur == "" || _nouvelleSaisie)
                _accumulateur = valeur;

            _operateur = op;
            _operation = $"{_accumulateur} {op}";
            _nouvelleSaisie = true;
        }
    }

    private void Calculer()
    {
        if (!double.TryParse(_saisie, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var valeur)) return;

        double resultat;
        switch (_operateur)
        {
            case "+": resultat = _accumulateur + valeur; break;
            case "-": resultat = _accumulateur - valeur; break;
            case "*": resultat = _accumulateur * valeur; break;
            case "/":
                if (valeur == 0)
                {
                    _saisie = "Division par zéro";
                    _operation = "";
                    _accumulateur = 0;
                    return;
                }
                resultat = _accumulateur / valeur;
                break;
            case "^": resultat = Math.Pow(_accumulateur, valeur); break;
            default: resultat = valeur; break;
        }

        _operation = $"{_accumulateur} {_operateur} {valeur} =";
        _accumulateur = resultat;
        _saisie = resultat.ToString(CultureInfo.InvariantCulture);
        _operateur = "";
    }

    // =========================================================
    //  AFFICHAGE
    // =========================================================
    private void MettreAJourAffichage()
    {
        LblOperation.Text = _operation;

        var texte = _saisie;
        LblResultat.Text = texte;

        // Réduit la police si le résultat est très long (responsive texte)
        LblResultat.FontSize = texte.Length switch
        {
            <= 8 => 48,
            <= 12 => 38,
            <= 16 => 30,
            _ => 24
        };
    }

    // =========================================================
    //  BOUTONS C / DEL (Labels cliquables)
    // =========================================================
    private void OnClearTapped(object? sender, TappedEventArgs e)
    {
        _operation = ""; _saisie = "0"; _accumulateur = 0;
        _operateur = ""; _nouvelleSaisie = true;
        MettreAJourAffichage();
    }

    private void OnDelTapped(object? sender, TappedEventArgs e)
    {
        if (_saisie.Length > 1 && _saisie != "Division par zéro")
            _saisie = _saisie[..^1];
        else
            _saisie = "0";
        MettreAJourAffichage();
    }
}