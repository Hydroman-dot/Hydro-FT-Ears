# Hydro FT Ears

Modulare Ohrbewegungen für VRChat: Die Ohren folgen der Blickrichtung und reagieren optional auf Smile, Frown und eine sanfte Idle-Bewegung. Die Einrichtung erfolgt in Unity unter **Hydro Tools → Hydro FT Ears**.

**Version 0.1.3 · Vom Entwickler im eigenen Avatar als funktionierend bestätigt.**

## Download und Installation

1. [HydroFTEars-0.1.3.unitypackage herunterladen](dist/HydroFTEars-0.1.3.unitypackage) und in ein vorbereitetes VRChat-Avatar-Projekt importieren.
2. Den Avatar auswählen und **Hydro Tools → Hydro FT Ears** öffnen.
3. Das modulare GameObject anlegen, oder `Prefabs/Hydro FT Ears.prefab` unter den Avatar ziehen.
4. Ohr-Bones und FT-Parameter automatisch zuordnen lassen und kontrollieren. Unbekannte Bones und Parameter können manuell zugewiesen werden.
5. Mit der Live-Vorschau Drehrichtung und Stärke abstimmen. Anschließend die Vorschau stoppen.
6. **Controller und VRChat-Menü erstellen / aktualisieren** anklicken, die Szene speichern und den Avatar neu bauen beziehungsweise hochladen.

Nach Änderungen an Bones, Parametern oder Winkeln erneut generieren. Dies ist ein Asset-Repository, kein vollständiges Unity-Projekt.

## Funktionen

- Automatische Erkennung bekannter Ohr-Bones und FT-Parameter, mit manueller Zuordnung.
- Liest Controller aus Avatar-Layern, Modular Avatar und VRCFury-Konfigurationen.
- Subtiles Eye Follow horizontal und vertikal, getrennt pro Ohr einstellbar.
- Optionale Smile-/Frown-Beiträge und Idle-Bewegung.
- Getrennte additive Layer und BlendTrees kombinieren die Beiträge mit vorhandenen Ohranimationen.
- Enable-Toggle und gespeicherter Intensitätsregler im VRChat-Menü.
- Live-Vorschau mit Wiederherstellung der ursprünglichen Ohrpose.
- Eigener Paketexport ohne Avatar- oder FT-Assets anderer Anbieter.

## Intensität: Radialregler ×5

Der VRChat-Regler zeigt weiterhin **0–100 %**, seine Wirkung wird fünffach skaliert:

| Regler in VRChat | Ohrbewegungsstärke | Wert im Unity-Wizard |
| --- | --- | --- |
| 0 % | aus | 0 |
| 20 % | normal, 100 % | 1 |
| 60 % | dreifach, 300 % | 3 |
| 100 % | fünffach, 500 % | 5 |

Im Menü heißt der Regler **Intensity (x5)**. Bereits in VRChat gespeicherte Werte können nach einem Versionswechsel neu eingestellt werden müssen. Zwei eigene synchronisierte Parameter benötigen insgesamt 9 Bits; das Gesamtbudget hängt vom Avatar ab.

## Smile und Frown abschalten

Im Wizard die Einträge **Smile/Frown links**, **Smile/Frown rechts** sowie gegebenenfalls **Separates Frown links/rechts** über **… → (aus)** leeren. Anschließend den Controller aktualisieren.

Damit entfallen die zusätzlichen Hydro-Ausdrucksbewegungen. Bereits vorhandene Smile-/Frown-Ohranimationen des FT-Systems bleiben erhalten.

## Voraussetzungen und Kompatibilität

SDK und Modular Avatar müssen vor dem Import installiert sein. Im Entwicklungsprojekt geprüft:

| Komponente | Geprüfte Version |
| --- | --- |
| Unity | 2022.3.22f1 |
| VRChat Avatars SDK | 3.10.5 |
| Modular Avatar | 1.18.2 |
| NDMF | 1.14.8 |
| VRCFury für das Echo-Preset | 1.1426.0 |

Echo FT auf Rindo war das erste geprüfte Setup. Andere Avatare benötigen passende Ohr-Bones und erreichbare Float-Parameter. FT-Eingänge sind für Blickrichtung und kombinierte Smile/Frown-Signale auf −1 bis 1 ausgelegt; getrennte Smile-/Frown-Signale verwenden 0 bis 1. Echo FT und VRCFury sind für andere FT-Systeme keine generelle Voraussetzung.

Private oder beim Build umbenannte VRCFury-Parameter müssen unter den zugeordneten Namen zugänglich gemacht werden. Die Erkennung kann nicht jede beliebige Umbenennung auflösen. PhysBone-Einstellungen werden nicht verändert; animierte Ohren müssen durch ihr PhysBone-Setup zugelassen werden.

## Keine Laufzeit-Scripts im Avatar

Die Hydro-Komponente speichert die Konfiguration in Unity und ist als `IEditorOnly` markiert. Wizard und Build-Korrektur liegen im `Editor`-Ordner. Beim SDK-Build wird die Hydro-Komponente entfernt. In VRChat übernehmen ausschließlich Animator-Layer, BlendTrees, Animationsclips und Parameter die Ohrbewegung.

Version 0.1.3 ordnet die Hydro-Layer nach dem Zusammenführen durch Modular Avatar und VRCFury hinter den anderen FX-Layern an. So überschreiben spätere Echo-Ohranimationen den Hydro-Beitrag nicht mehr. Betroffene Layer-Control- und Sync-Layer-Indizes werden dabei angepasst.

## Quellcode und Beispiele

```text
Assets/HydroTools/HydroFTEars/
  Runtime/   Konfigurationskomponente, beim Build entfernt
  Editor/    Wizard, Generator, Build-Korrektur und Diagnosetests
  Prefabs/   Installations-Prefab
  Samples/   Eigene generische Zweibone-Rig und Beispiel-Animator
dist/        Importierbares Unity-Paket und SHA-256-Prüfsumme
```

Die Beispiel-Rig ist kein hochladbarer Avatar. Avatarspezifische Ergebnisse entstehen lokal unter `Generated/` und werden nicht versioniert. Das Paket enthält keine Rindo-, Echo-, VRCFury- oder Modular-Avatar-Assets; diese Abhängigkeiten werden separat installiert.

## Verifikation

- Unity-Kompilierung ohne Scriptfehler.
- Isolierte Animator-Tests für Neutralpose, Eye Follow, vertikale Bewegung, kombinierte Beiträge, Intensität, Aus und Idle.
- Live-Vorschau stellt beide Ohrrotationen wieder her.
- Vollständiges SDK-Preprocessing am Referenzavatar erfolgreich; danach keine Hydro-Komponenten vorhanden.
- Signaltest am zusammengeführten FX-Controller: rohe Echo-Eingänge mit Wert 1 ergeben geglättete Proxy-Werte von 1. Vor der Reihenfolgekorrektur: 0° Hydro-Bewegung; danach: etwa 35,76° bei maximaler x5-Stärke.
- Der Anwender hat die Funktion am 1. Oktober 2026 bestätigt. Eine allgemeine Garantie für andere Avatare oder Paketversionen ist daraus nicht ableitbar.

Der Signaltest ersetzt keinen OSC-Empfangstest. Der Eye-Puppet-Menüpfad wurde nicht separat geprüft. Die Editor-Vorschau simuliert Eingaben und keine vollständige PhysBone-Dynamik.

## Änderungen

Siehe [CHANGELOG.md](CHANGELOG.md). Fehlerberichte bitte mit Unity-/SDK-/MA-/VRCFury-Version, verwendeten Parametern und reproduzierbaren Schritten einreichen; keine gekauften Avatar-Assets hochladen.

Für dieses Repository wurde noch keine Open-Source-Lizenz festgelegt.

