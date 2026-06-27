# Frontier

Frontier is a Unity 2022.3 card-battler prototype moving toward a KARDS-like tabletop UX: two headquarters, escalating Kredits, a hand of cards, support-line deployment, frontline movement, attacks, and simple board feedback.

## Run in Unity

- Open `Assets/Scenes/SampleScene.unity` in Unity 2022.3.62f3c1 and press Play; the scene now starts directly in a playable player turn with hands, deck counts, slots, and current turn controls visible.
- The scene contains authored `GameController`, `Board_MA`, `FeedbackManager`, `Card Library`, and `Main Camera` objects wired together for the Game window; the runtime bootstrap remains as a fallback if a scene is opened without those objects.
- `Card Library` keeps authored `CardData` assets available in the scene; its non-interactive runtime gallery is optional and disabled in the main scene by default.
- Edit mode shows the same playable tabletop composition as Play mode: dark table material, generated board slots, slot-like headquarters plates, preview hands, current-turn command buttons, and matching camera framing.
- The scene `GameController` uses authored `CardData` references for player and enemy decks; those asset lists are expanded into 40-card playable decks at match start.
- Authored materials under `Assets/Materials` now drive board line color and card gallery faction/countermeasure styling through `SceneVisualStyle`.
- Authored board labels and HQ anchors live under `Board_MA`; generated slots preserve those scene-authored presentation objects at Play.
- Authored `Card Layout` anchors drive hand and countermeasure card placement, so layout tuning happens in the Unity scene instead of hard-coded coordinates.
- Authored pile displays show player/enemy deck and discard counts in the 3D scene, updated from gameplay state.

## Current milestone

Implemented in this workspace:

- Runtime board generation with enemy support, frontline, and player support rows.
- Runtime starter deck selection with four 40-card archetypes, collection previews, editable card counts, three saved deck slots, text search, type/faction/rarity filters, and local deck-list persistence.
- Authored `CardData` assets under `Assets/Cards` for inspecting deployment effects, countermeasures, pinning, keywords, and faction metadata in Unity.
- Authored board/card materials are wired into the scene so visual styling can be inspected and tuned from Unity instead of only generated in code.
- Authored board presentation markers keep lane labels and headquarters anchors visible/editable in the scene hierarchy.
- Authored card layout anchors control player/enemy hand and countermeasure rows.
- Authored pile display anchors expose deck and discard counts outside the IMGUI status panel.
- A playable scene presenter frames the match as a dark grooved tabletop, reduces background labels, and keeps cards/slots/buttons as the primary visual shapes instead of a white text-heavy board.
- The tabletop now has a darker raised border and clearer military-panel contrast so the board reads less like a flat white prototype.
- Headquarters now render as clickable and draggable-target slot-like support-line objects with large HQ numerals instead of floating labels.
- The player hand rests mostly below the bottom edge and slides into view while the mouse is near the bottom hand rail, matching the hidden-hand tabletop pattern.
- Player hand cards are spaced farther apart and scaled up compared with earlier prototype values so card names and costs are easier to read during first-time play.
- Cards use smaller labelled rules text, larger cost/attack/defense numerals, labelled numeric badges, bigger art panels, and explicit `STATUS:` badges for pinned, guard, blitz, ambush, and countermeasure states.
- Set player countermeasures are larger, remain clickable, and now show a checking-only prompt instead of asking players to set the same countermeasure again.
- A centered action prompt now names the current step, such as choosing a deck, keeping or mulliganing, playing to support, waiting through the enemy turn, or restarting after game over.
- When a card is selected, the top HUD switches from the generic turn prompt to a concrete instruction such as `CLICK DEPLOY HERE`, `CLICK ADVANCE HERE`, or `CLICK ATTACK HERE`.
- The top HUD now uses the same frontline-control context as the status panel, so it does not offer impossible advance or Mobilize actions.
- After deploying, setting a countermeasure, playing an order, advancing, or attacking, the status text now includes a `NEXT:` hint so players know whether to advance, attack, play another card, or end the turn.
- An edit-mode scene preview mirrors the playable layout before pressing Play, so slot arrangement, colors, card shapes, and command controls can be tuned directly in the editor.
- A scene-authored command status display mirrors HQ, Kredits, phase, frontline control, current status, and recent actions directly on the tabletop.
- The status display now labels `FRONTLINE: YOU/ENEMY/NEUTRAL` and explains when enemy frontline control must be cleared before advancing.
- Clickable tabletop command buttons now render as physical, phase-aware plates and drive start match, mulligan decisions, end turn, restart, and board strike actions without requiring the IMGUI overlay.
- Visible disabled command buttons still forward clicks to the status panel, so players get a reason instead of a silent no-op.
- Clickable tabletop deck selectors choose the starter archetype before the match starts.
- A scene-authored deck summary shows the selected archetype, deck slot, source, readiness, and a concrete `NEXT:` line telling players to click a faction plate or `Start Match`.
- A scene-authored card inspector mirrors selected card cost, stats, zone, keywords, and rules on the tabletop.
- Card inspector text now uses player-facing labels for zones, effects, and keyword statuses instead of raw enum names.
- Scene-assigned authored card assets can drive the playable decks, cycling the assigned assets up to the 40-card deck requirement.
- The scene card gallery is authored but optional at runtime, keeping the playable Game view clear while retaining library references in the scene.
- The legacy IMGUI overlay is hidden by default; press `F1` in Play mode to reveal it for debugging.
- Opening hand flow stops on a visible `Inspect hand / Keep / Mulligan` decision before the first turn; after using the one mulligan, prompts switch to `Keep Hand` only.
- Unit deployment from hand to support line.
- Selecting a hand card that costs more Kredits than available now shows the required Kredit count instead of highlighting impossible targets.
- Clicking an illegal order target now explains the reason, including Smokescreen blocking order targeting, enemy-only pin/damage targets, and friendly-only buff targets.
- Deployment prompts distinguish normal units that wait until next turn from Blitz units that can act immediately.
- Deployment effects for units that trigger when played.
- Unit advancement from support line to frontline using operation cost.
- Failed advance or attack attempts now explain the exact blocker: missing operation Kredits, pinned status, already acted, or already attacked.
- Support-line units that are used as attackers now explain that they must advance to the frontline before attacking.
- Selecting pinned, spent, or unaffordable deployed units now shows the blocker immediately instead of asking for impossible advance or attack targets.
- Selecting a support unit while the enemy controls the frontline now tells players to clear the frontline before advancing.
- Selecting a Mobilize unit in hand now tells players it can deploy to support or mobilize directly to frontline.
- Selecting a Mobilize unit while the enemy controls the frontline now keeps support deployment visible but explains that frontline mobilizing is blocked.
- Illegal deployment targets now explain occupied slots, support-only units, and Mobilize frontline-control restrictions.
- The `P` quick-play fallback now reports whether it failed because the hand is empty, Kredits are short, support is full, or orders lack legal targets.
- The `A` quick-advance fallback now reports whether it failed because there is no support unit, not enough Kredits, pinned/spent units, enemy frontline control, or no empty frontline slot.
- The `F` quick-attack fallback now reports whether it failed because there is no frontline unit, not enough Kredits, pinned/spent attackers, or no legal target.
- Advance prompts now check remaining Kredits before telling players to attack, so costly units show the attack requirement instead of impossible targets.
- Attack prompts call out Fury units that can attack again with the same card, including the Kredit requirement when the second attack is unaffordable.
- Attack resolution uses a tested attack-count rule so normal units spend after one attack and Fury units spend after their second attack.
- Frontline control: only the controlling side can advance new units until the line is cleared.
- Frontline attacks against enemy support units or HQ, with guard targeting restrictions.
- Illegal attack targets now explain whether the player clicked the wrong lane, a friendly card, or must attack Guard first.
- Orders for direct damage, unit damage, HQ repair, card draw, buffs, and pinning that skips a unit's next operation window.
- Countermeasures that can be set from hand; the player's own set countermeasures stay inspectable while enemy countermeasures remain hidden until triggered.
- Unit keywords: Blitz, Guard, Fury, Smokescreen, Ambush, Mobilize, Heavy Armor, and Pinned.
- Scored enemy AI that chooses orders, deployment, and attacks from board state instead of always taking the first available action.
- Clickable and draggable 3D card and slot visuals with highlighted legal targets.
- Highlighted legal target slots now show action labels such as `DEPLOY HERE`, `ADVANCE HERE`, `ATTACK HERE`, `ATTACK HQ`, `DAMAGE UNIT`, `PIN UNIT`, `BUFF ALLY`, `REPAIR HQ`, `DRAW CARDS`, and `SET COUNTER`, so first-time players do not have to infer clickability from color alone.
- Selecting a frontline unit now says `CLICK ATTACK TARGET OR HQ`, matching the separate `ATTACK HERE` and `ATTACK HQ` target labels.
- When Guard blocks attacks, highlighted Guard targets use `ATTACK GUARD` so players understand why other units or HQ are not selectable.
- Card detail inspector for selected visible cards.
- Action log for recent deploys, orders, attacks, countermeasures, and turn flow.
- Queued floating combat feedback, procedural pulse VFX, generated audio cues, hover/selection card motion, spawn pops, and attack tracers for combat feedback, with input gated while events resolve.
- Runtime board labels for support rows, frontline, and headquarters zones.
- Faction-tinted cards with rarity bands for collection and battlefield readability.
- Explicit victory/defeat/draw game-over states with restart.
- Strike/ripple board feedback and camera shake hook.

## Controls

- Choose deck slot 1–3, pick a starter archetype, search and filter by type/faction/rarity, adjust card counts with `+`/`-`, optionally enable `Use edited deck` once it has at least 40 cards, then use `Start Match`.
- Use `Keep Hand` to start, or `Mulligan` once to redraw the opening hand.
- Move the mouse near the bottom hand rail to reveal your hand; move away to tuck it partly out of view.
- Click any visible card, including your set countermeasure cards, to inspect full labelled details in the card panel.
- Click or drag a player unit card onto an empty blue support slot to deploy it.
- Click or drag an order card onto a highlighted target if the order needs one; non-targeted orders can be played on any slot.
- Click or drag a countermeasure card onto any board slot to set it face-down.
- If deployment, order play, or countermeasure setting is unaffordable, the prompt names the card, required Kredits, and current Kredits.
- Click or drag a deployed support unit onto an empty yellow frontline slot to advance it.
- Click or drag a frontline unit onto a highlighted red enemy support slot to attack a unit or the enemy HQ.
- Use `End Turn` to pass to the enemy AI.
- Keyboard fallback: `P` plays the first legal card, `A` advances the first ready support unit, `F` attacks with the first ready frontline unit, and `Space` ends the turn.
- Use `Restart` after victory, defeat, or draw.
- Use `Strike Board` to preview board impact feedback.

## Scope notes

This project does not include KARDS assets, branding, card database, monetization, networking, or exact UI art. The implementation is an original foundation for similar mechanics and UX patterns inside this Unity project.

## Next implementation targets

- Replace generated placeholder visuals with proper canvas/world-space prefabs, illustrated card art, polished frames, and animated drag previews.
- Expand order and countermeasure timing windows.
- Add additional keywords, nation identities, timing restrictions, and richer frontline interactions.
- Expand the current saved starter-deck editor into a full collection screen with ownership, import/export, card art, rarity treatment, and named deck metadata.
- Replace the lightweight resolution queue and procedural cue layer with fully authored card movement, combat VFX, sound design, and polished game state panels.
