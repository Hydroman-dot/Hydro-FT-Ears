# Hydro FT Ears — 0.1.3

Ein modulares, im Editor erzeugtes Ohr-Addon für VRChat. Keine Laufzeit-Scripts im Avatar erforderlich: Die Hydro-Konfigurationskomponente ist IEditorOnly; im Avatar arbeiten Animator, AnimationClips und Modular Avatar.

## Voraussetzungen
Im Entwicklungsprojekt geprüft: Unity 2022.3.22f1, VRChat Avatars SDK 3.10.5, Modular Avatar 1.18.2 / NDMF 1.14.8. Echo FT wird im Referenzprojekt durch VRCFury 1.1426.0 eingebunden. SDK und Modular Avatar müssen vor dem Paket installiert sein. Echo FT / VRCFury sind keine Pflicht für andere FT-Systeme.

## Einrichtung
1. Avatar auswählen und **Hydro Tools → Hydro FT Ears** öffnen.
2. **Modulares GameObject anlegen**, oder das Prefab unter Prefabs unter den Avatar ziehen.
3. Automatische Erkennung prüfen; linkes und rechtes Ohr bei Bedarf manuell zuweisen.
4. FT-Parameter über die Auswahl rechts oder als exakten Namen zuweisen. Leeres Feld deaktiviert den Beitrag. Unterstützt werden Float-Eingänge: Blick −1…1, Smile/Frown −1…1 oder separate positive Smile/Frown-Signale 0…1.
5. Die lokale Drehrichtung und Stärke pro Ohr in der Live-Vorschau prüfen. Die Automatik richtet Blick-Yaw/Pitch an Avatar-Up/Right aus; andere Posen sind anpassbare Startwerte.
6. Vorschau stoppen; **Controller und VRChat-Menü erstellen / aktualisieren** drücken.
7. Szene speichern. Nach Bone-/Pfad-/Parameter-/Winkeländerungen erneut generieren.

Die Vorschau simuliert Eingaben und verändert vorübergehend nur die beiden Ohrrotationen. Sie stellt die Ausgangspose beim Stoppen, Schließen, Neuladen und Play-Mode-Wechsel wieder her. Sie simuliert keine PhysBone-Dynamik, keine komplette Echo-Animator-Auswertung und keinen OSC-Empfang.

## Aufbau
Das GameObject enthält die Konfiguration, MA Merge Animator (FX, absolute Pfade, Priorität 100), MA Menu Installer und MA Parameters. Der Generator erzeugt eigene Clips mit expliziter additiver Referenzpose, getrennte additive Eye-X-links/rechts-, Eye-Y-, Expression-links/rechts- und Idle-Layer. Separate Frown-Layer kommen bei entsprechender Zuordnung hinzu.

Jeder Beitrag wird unabhängig durch einen Intensitäts-BlendTree skaliert; ein Enable-Zustandswechsel blendet ihn aus. Fremde Controller und Clips werden nicht geändert. Bestehende Echo-Ohrhaltungen bleiben deshalb als darunterliegende Animation erhalten. Smile/Frown hier ist eine zusätzliche, anpassbare Reaktion. Auf null gesetzte Winkel unterdrücken eine unerwünschte Verstärkung vorhandener Posen.

Idle ist eine eigene sanfte periodische Bewegung. Ein optionaler Float-Parameter kann ihre Stärke skalieren; ohne Zuordnung läuft sie bei aktiviertem Modul mit. Ein vorhandener Idle-State muss nicht ersetzt oder kopiert werden.

VRChat-Untermenü: **Hydro FT Ears → Enabled / Intensity**.
Eigene gespeicherte, synchronisierte Parameter:
- Hydro/FTEars/Enabled — Bool, Standard an.
- Hydro/FTEars/Intensity — Float 0…1, Standard 0,65.
Zusätzlicher Parameterbedarf: 9 Bits. Das Gesamtbudget des vollständigen Avatars muss beim SDK-Build passen.

## FT-Erkennung
Die Erkennung liest Avatar-Layer, Animator-Komponenten, MA Merge Animator und die serialisierten Controller-Verweise von VRCFury. Sie bevorzugt bekannte geglättete Echo-Parameter, danach bekannte rohe Echo-/Unified-Namen. Eindeutige Standardnamen für Ohr-Bones werden erkannt. Unbekannte Namen sind manuell zuweisbar. Die eigenen generierten Controller werden nicht als FT-Quelle gewertet.

Die Parameter müssen beim Build unter den zugewiesenen Namen erreichbar bleiben. Im untersuchten Echo-Preset sind VRCFury globalParams auf * gesetzt. Andere VRCFury-Presets mit privaten umbenannten Parametern brauchen entsprechend freigegebene Namen; automatische Auflösung beliebiger Build-Umbenennungen ist nicht enthalten.

## Dateien und Weitergabe
- Runtime/HydroFTEars.cs: Editor-only Konfiguration am GameObject.
- Editor/: Wizard, Generator und isolierter Animator-Test.
- Prefabs/Hydro FT Ears.prefab: unkonfiguriertes Installations-Prefab.
- Samples/: komplett selbst erzeugte Zweibone-Beispiel-Rig mit eigenen Eingangsdeklarationen, Clips, Animator und Menüs; kein hochladbarer Avatar.
- Generated/: lokale, avatarspezifische Ergebnisse. Jede Aktualisierung erzeugt einen neuen Ordner, damit vorherige Ergebnisse nicht überschrieben werden.

**Eigenes .unitypackage exportieren** exportiert die eigenen Werkzeug- und Beispiel-Dateien ohne IncludeDependencies. Generated und fremde Avatar-/FT-Assets sind ausgeschlossen. Für einen anderen Avatar werden seine Pfade und Referenzpose im Wizard neu erzeugt. Die Beispiele enthalten keine Rindo-/Echo-Assets.

## Prüfung und Grenzen
Die erzeugten Controller wurden im Unity-PlayableGraph auf einer isolierten Zweibone-Hierarchy geprüft: Neutralpose, Eye Follow, gleichzeitige Expression + Eye Follow, 0 %, Aus und 50 % Intensität. Dieselben Tests wurden mit der generischen Beispiel-Rig durchgeführt. Kompilierung ohne Scriptfehler.

Ein vollständiger VRChat-SDK-Upload und ein Headset-/OSC-End-to-End-Test wurden nicht durchgeführt. Bone-Achsen, PhysBone-Verhalten und die endgültige Layer-Reihenfolge nach allen Drittanbieter-Buildschritten sollten beim Avatar-Test überprüft werden. Das Werkzeug ändert PhysBone-Einstellungen nicht.

Technische Referenz: https://modular-avatar.nadena.dev/docs/reference/merge-animator


## Update 0.1.2 — Fünffache Radial-Skalierung
Der VRChat-Radialregler zeigt weiterhin 0–100%. Seine Wirkung wird fünffach skaliert: 1% Regler = 5% Wirkung, 20% = normale Stärke, 60% = dreifache Stärke, 100% = fünffache Stärke. Standardintensität und Live-Vorschau im Wizard gehen von 0 bis 5. Standardwert 1 wird als Radialwert 0,2 gespeichert. Menü: Intensity (x5). Vorhandene gespeicherte VRChat-Reglerpositionen müssen gegebenenfalls neu eingestellt werden.

## Update 0.1.3 — Echo überschreibt Ohrbewegung: behoben
Nach MA und VRCFury verschiebt ein ausschließlich im Editor laufender SDK-Build-Callback die additiven Hydro-Layer hinter die anderen FX-Layer. Vorhandene Layer-Control-Indizes und Sync-Layer-Indizes werden angepasst. Das Build-Ergebnis enthält keine Hydro-Komponenten oder Hydro-Laufzeit-Scripts.
Vollständiges SDK-Preprocessing am Rindo erfolgreich. Signaltest am fertigen Controller: EchoFT/v2/EyeLeftX=1 und EyeRightX=1 führen zu OSCm/Proxy=1. Alte Reihenfolge: 0 Grad Hydro-Reaktion; korrigierte Reihenfolge: 35,763 Grad bei maximaler x5-Stärke. Der Test setzt die rohen Echo-Eingänge, nicht die Proxy-Ausgänge. Ein Headset-Test bleibt noch aus.
Die Korrektur wird beim nächsten Build/Upload angewendet. Die Vorschau eines lediglich manuell zusammengeführten NDMF-Avatars ohne SDK-Preprocessing ist nicht gleichbedeutend mit diesem SDK-Test.
