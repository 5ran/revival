# Macro C++ Migration Tracker

## Completed
1. Rod classification module ported from C# to C++.
   - Source: `soruce/Services/Fishing/Rod/RodKind.cs`
   - Source: `soruce/Services/Fishing/Rod/RodClassifier.cs`
   - Output:
     - `cpp-macro-port/include/rod_kind.hpp`
     - `cpp-macro-port/include/rod_classifier.hpp`
     - `cpp-macro-port/src/rod_classifier.cpp`
     - `cpp-macro-port/tests/rod_classifier_test.cpp`
2. Fishing hold gate ported from C# to C++.
   - Source: `soruce/Services/Fishing/Rod/FishingHoldGate.cs`
   - Output:
     - `cpp-macro-port/include/fishing_hold_gate.hpp`
     - `cpp-macro-port/src/fishing_hold_gate.cpp`
     - `cpp-macro-port/tests/fishing_hold_gate_test.cpp`
3. Rod profile base/factory + core profiles ported from C# to C++.
   - Source: `soruce/Services/Fishing/Rod/RodProfile.cs`
   - Source: `soruce/Services/Fishing/Rod/*RodProfile.cs`
   - Source: `soruce/Services/Fishing/Rod/NoteTarget.cs`
   - Output:
     - `cpp-macro-port/include/rod_profile.hpp`
     - `cpp-macro-port/include/reel_metrics.hpp`
     - `cpp-macro-port/include/note_target.hpp`
     - `cpp-macro-port/src/rod_profile.cpp`
     - `cpp-macro-port/tests/rod_profile_test.cpp`
4. Tracking1 controller decision loop ported (pure logic version).
   - Source: `soruce/Services/Fishing/Tracking1/Tracking1Controller.cs`
   - Source: `soruce/Services/Fishing/Tracking1/Tracking1Settings.cs`
   - Output:
     - `cpp-macro-port/include/tracking1_settings.hpp`
     - `cpp-macro-port/include/tracking1_controller.hpp`
     - `cpp-macro-port/src/tracking1_controller.cpp`
     - `cpp-macro-port/tests/tracking1_controller_test.cpp`
5. Tracking2 controller decision loop ported (pure logic version).
   - Source: `soruce/Services/Fishing/Tracking2/Tracking2Controller.cs`
   - Source: `soruce/Services/Fishing/Tracking2/Tracking2Settings.cs`
   - Output:
     - `cpp-macro-port/include/tracking2_settings.hpp`
     - `cpp-macro-port/include/tracking2_controller.hpp`
     - `cpp-macro-port/src/tracking2_controller.cpp`
     - `cpp-macro-port/tests/tracking2_controller_test.cpp`
6. Tracking3 controller decision loop ported (pure logic version).
   - Source: `soruce/Services/Fishing/Tracking3/Tracking3Controller.cs`
   - Source: `soruce/Services/Fishing/Tracking3/Tracking3Settings.cs`
   - Source: `soruce/Services/Fishing/Tracking3/Tracking3Decision.cs`
   - Output:
     - `cpp-macro-port/include/tracking3_settings.hpp`
     - `cpp-macro-port/include/tracking3_controller.hpp`
     - `cpp-macro-port/src/tracking3_controller.cpp`
     - `cpp-macro-port/tests/tracking3_controller_test.cpp`
7. Input actuator interface and hold application bridge added.
   - Source pattern: `Tracking1FishingTracker.ApplyFishingHold/ReleaseFishingHold` + `NativeMouse`
   - Output:
     - `cpp-macro-port/include/input_actuator.hpp`
     - `cpp-macro-port/include/hold_applier.hpp`
     - `cpp-macro-port/src/hold_applier.cpp`
     - `cpp-macro-port/tests/hold_applier_test.cpp`
8. Runtime memory abstraction + core fishing runtime context ported.
   - Source: `soruce/Services/Fishing/Runtime/FishingRuntimeContext.cs`
   - Source: `soruce/Services/Fishing/Runtime/RobloxMemory.cs`
   - Source: `soruce/Services/Fishing/Runtime/IOffsetsSource.cs`
   - Output:
     - `cpp-macro-port/include/offsets_source.hpp`
     - `cpp-macro-port/include/roblox_memory.hpp`
     - `cpp-macro-port/include/fishing_runtime_context.hpp`
     - `cpp-macro-port/src/fishing_runtime_context.cpp`
     - `cpp-macro-port/tests/fishing_runtime_context_test.cpp`
9. Tracker orchestration phase machine ported (pure logic).
   - Source pattern: `Tracking*FishingTracker` phase loop (`CASTING`/`CASTED`/`SHAKE`/`FISHING`)
   - Output:
     - `cpp-macro-port/include/fishing_phase.hpp`
     - `cpp-macro-port/include/tracker_orchestrator.hpp`
     - `cpp-macro-port/src/tracker_orchestrator.cpp`
     - `cpp-macro-port/tests/tracker_orchestrator_test.cpp`
10. Concrete Win32 Roblox memory backend added (read-only).
   - Source: `soruce/Services/Fishing/Runtime/RobloxMemory.cs`
   - Output:
     - `cpp-macro-port/include/win32_roblox_memory.hpp`
     - `cpp-macro-port/src/win32_roblox_memory.cpp`
     - `cpp-macro-port/tests/win32_roblox_memory_offsets_test.cpp`
11. Tracker-to-runtime integration facade added.
   - Source pattern: `Tracking1FishingTracker` per-tick orchestration across runtime context, phase machine, controller, and input gate.
   - Output:
     - `cpp-macro-port/include/runtime_tracker_engine.hpp`
     - `cpp-macro-port/src/runtime_tracker_engine.cpp`
     - `cpp-macro-port/tests/runtime_tracker_engine_test.cpp`
12. Bellona dual-reel right-side controller + runtime integration added.
   - Source pattern: `Tracking1FishingTracker.UpdateBellonaRightHold` and related debounce/grace/watchdog behavior.
   - Output:
     - `cpp-macro-port/include/bellona_dual_controller.hpp`
     - `cpp-macro-port/src/bellona_dual_controller.cpp`
     - `cpp-macro-port/tests/bellona_runtime_engine_test.cpp`
13. Runtime engine mode routing added for Tracking1/Tracking2/Tracking3.
   - Source pattern: `Tracking1FishingTracker` mode-switch behavior for controller selection.
   - Output:
     - `cpp-macro-port/include/runtime_tracker_engine.hpp` (mode/settings expansion)
     - `cpp-macro-port/src/runtime_tracker_engine.cpp` (left/right mode-based decision routing)
     - `cpp-macro-port/tests/runtime_tracker_modes_test.cpp`
14. Startup-assist branch integrated into runtime engine fishing flow.
   - Source pattern: early-fishing startup assist behavior in `Tracking1FishingTracker`.
   - Output:
     - `cpp-macro-port/include/runtime_tracker_engine.hpp` (startup-assist settings/result flag)
     - `cpp-macro-port/src/runtime_tracker_engine.cpp` (timed startup-assist controller override)
     - `cpp-macro-port/tests/runtime_startup_assist_test.cpp`
15. Perfect-cast release behavior expanded in orchestrator.
   - Source pattern: perfect-cast target release + near-target micro-burst behavior.
   - Output:
     - `cpp-macro-port/include/tracker_orchestrator.hpp` (perfect-cast settings, cast power percent input)
     - `cpp-macro-port/src/tracker_orchestrator.cpp` (target/near-window release + timeout release mode)
     - `cpp-macro-port/tests/tracker_orchestrator_perfect_cast_test.cpp`
16. Runtime perfect-cast burst cache parity added.
   - Source pattern: short burst sampling continuity when power bar flickers.
   - Output:
     - `cpp-macro-port/include/runtime_tracker_engine.hpp` (perfect-cast cache settings/state)
     - `cpp-macro-port/src/runtime_tracker_engine.cpp` (effective cast power readiness/percent cache)
     - `cpp-macro-port/tests/runtime_perfect_cast_cache_test.cpp`
17. Auto Totem boundary gate logic ported and wired into runtime tick status.
   - Source pattern: `ComputeAutoTotemBoundary` + post-catch settle gate signaling.
   - Output:
     - `cpp-macro-port/include/auto_totem_boundary.hpp`
     - `cpp-macro-port/src/auto_totem_boundary.cpp`
     - `cpp-macro-port/tests/auto_totem_boundary_test.cpp`
     - `cpp-macro-port/tests/runtime_automation_gate_test.cpp`
18. Auto Totem workflow state sequencing ported (pending/await/retry core).
   - Source pattern: `UpdateAutoTotem` state transitions around pending queue, boundary run, await-fish-cycle gate, and retry cooldown.
   - Output:
     - `cpp-macro-port/include/auto_totem_workflow_state.hpp`
     - `cpp-macro-port/src/auto_totem_workflow_state.cpp`
     - `cpp-macro-port/tests/auto_totem_workflow_state_test.cpp`
19. Auto Totem workflow state integrated into runtime engine facade.
   - Source pattern: tracker-level add-on gating decisions surfaced per tick.
   - Output:
     - `cpp-macro-port/include/runtime_tracker_engine.hpp` (auto-totem input/output hooks)
     - `cpp-macro-port/src/runtime_tracker_engine.cpp` (per-tick workflow state update)
     - `cpp-macro-port/tests/runtime_auto_totem_workflow_integration_test.cpp`
20. Auto Totem workflow callback result model expanded.
   - Source pattern: distinct outcomes for workflow execution (success, blocked by gate, active-state unreadable, failed) instead of a single boolean.
   - Output:
     - `cpp-macro-port/include/auto_totem_workflow_state.hpp`
     - `cpp-macro-port/src/auto_totem_workflow_state.cpp`
     - `cpp-macro-port/include/runtime_tracker_engine.hpp`
     - `cpp-macro-port/src/runtime_tracker_engine.cpp`
     - `cpp-macro-port/tests/auto_totem_workflow_state_test.cpp`
     - `cpp-macro-port/tests/runtime_auto_totem_workflow_integration_test.cpp`
21. Runtime facade external workflow executor plumbing added.
   - Source pattern: bridge runtime state machine with real external workflow execution callbacks.
   - Output:
     - `cpp-macro-port/include/auto_totem_workflow_executor.hpp`
     - `cpp-macro-port/include/runtime_tracker_engine.hpp`
     - `cpp-macro-port/src/runtime_tracker_engine.cpp`
     - `cpp-macro-port/tests/runtime_auto_totem_executor_test.cpp`
22. Multi-add-on input gate arbitration added around Auto Totem workflow start.
   - Source pattern: `AutomationInputGate` ownership checks and blocked-start requeue behavior.
   - Output:
     - `cpp-macro-port/include/automation_input_gate.hpp`
     - `cpp-macro-port/src/automation_input_gate.cpp`
     - `cpp-macro-port/include/runtime_tracker_engine.hpp`
     - `cpp-macro-port/src/runtime_tracker_engine.cpp`
     - `cpp-macro-port/tests/automation_input_gate_test.cpp`
     - `cpp-macro-port/tests/runtime_auto_totem_gate_block_test.cpp`
23. Replay/parity runner scaffold added for recorded tick comparisons.
   - Source pattern: side-by-side tick replay and field-level mismatch reporting.
   - Output:
     - `cpp-macro-port/include/replay_runner.hpp`
     - `cpp-macro-port/src/replay_runner.cpp`
     - `cpp-macro-port/tests/replay_runner_test.cpp`
24. End-to-end parity runner integration for recorded CSV tick logs added.
   - Source pattern: trace file loader + replay execution + report output.
   - Output:
     - `cpp-macro-port/include/replay_trace_io.hpp`
     - `cpp-macro-port/src/replay_trace_io.cpp`
     - `cpp-macro-port/include/parity_runner.hpp`
     - `cpp-macro-port/src/parity_runner.cpp`
     - `cpp-macro-port/tests/replay_trace_io_test.cpp`
     - `cpp-macro-port/tests/parity_runner_test.cpp`
25. Bellona single-reel side-assignment hysteresis ported.
   - Source pattern: `ResolveBellonaSideContexts` sticky left/right fallback behavior.
   - Output:
     - `cpp-macro-port/include/bellona_side_assigner.hpp`
     - `cpp-macro-port/src/bellona_side_assigner.cpp`
     - `cpp-macro-port/include/runtime_tracker_engine.hpp`
     - `cpp-macro-port/src/runtime_tracker_engine.cpp`
     - `cpp-macro-port/tests/bellona_side_assigner_test.cpp`
26. Remaining runtime/add-on support modules ported into C++ scaffolds and logic units.
   - Source:
     - `soruce/Services/Fishing/Runtime/OffsetsSourceProvider.cs`
     - `soruce/Services/Fishing/WorldStatusReader.cs`
     - `soruce/Services/Fishing/Hotbar/HotbarRodReader.cs`
     - `soruce/Services/Fishing/Rod/HotbarRodResolver.cs`
     - `soruce/Services/Fishing/HotbarTotemReader.cs`
     - `soruce/Services/Fishing/Tranquility/TranquilityController.cs`
     - `soruce/Services/Fishing/AutoSovereignRechargeRunner.cs`
     - `soruce/Services/Fishing/Aquarium/AquariumSequenceRunner.cs`
     - `soruce/Services/Fishing/Angler/AutoAnglerRunner.cs`
     - `soruce/Services/Fishing/Enchant/AutoEnchantRunner.cs`
     - `soruce/Services/Fishing/Appraise/Tracking2AppraiseRunner.cs`
     - `soruce/Services/Fishing/BellonaDebugOverlayService.cs`
   - Output:
     - `cpp-macro-port/include/offsets_source_provider.hpp`
     - `cpp-macro-port/src/offsets_source_provider.cpp`
     - `cpp-macro-port/include/world_status_reader.hpp`
     - `cpp-macro-port/src/world_status_reader.cpp`
     - `cpp-macro-port/include/hotbar_slot_settings.hpp`
     - `cpp-macro-port/src/hotbar_slot_settings.cpp`
     - `cpp-macro-port/include/hotbar_rod_resolver.hpp`
     - `cpp-macro-port/src/hotbar_rod_resolver.cpp`
     - `cpp-macro-port/include/hotbar_rod_reader.hpp`
     - `cpp-macro-port/src/hotbar_rod_reader.cpp`
     - `cpp-macro-port/include/hotbar_totem_reader.hpp`
     - `cpp-macro-port/include/tranquility_controller.hpp`
     - `cpp-macro-port/src/tranquility_controller.cpp`
     - `cpp-macro-port/include/auto_sovereign_recharge_runner.hpp`
     - `cpp-macro-port/src/auto_sovereign_recharge_runner.cpp`
     - `cpp-macro-port/include/aquarium_sequence_runner.hpp`
     - `cpp-macro-port/src/aquarium_sequence_runner.cpp`
     - `cpp-macro-port/include/auto_angler_runner.hpp`
     - `cpp-macro-port/src/auto_angler_runner.cpp`
     - `cpp-macro-port/include/auto_enchant_runner.hpp`
     - `cpp-macro-port/src/auto_enchant_runner.cpp`
     - `cpp-macro-port/include/tracking2_appraise_runner.hpp`
     - `cpp-macro-port/src/tracking2_appraise_runner.cpp`
     - `cpp-macro-port/include/bellona_debug_overlay_service.hpp`
     - `cpp-macro-port/src/bellona_debug_overlay_service.cpp`
27. Remaining tracker-shell and treasure-module compatibility shims added.
   - Source:
     - `soruce/Services/Fishing/Tracking1/Tracking1FishingTracker.cs`
     - `soruce/Services/Fishing/Tracking2/Tracking2FishingTracker.cs`
     - `soruce/Services/Fishing/Tracking3/Tracking3FishingTracker.cs`
     - `soruce/Services/Fishing/Treasure/TreasureRefPort.cs`
   - Output:
     - `cpp-macro-port/include/tracking1_fishing_tracker.hpp`
     - `cpp-macro-port/src/tracking1_fishing_tracker.cpp`
     - `cpp-macro-port/include/tracking2_fishing_tracker.hpp`
     - `cpp-macro-port/src/tracking2_fishing_tracker.cpp`
     - `cpp-macro-port/include/tracking3_fishing_tracker.hpp`
     - `cpp-macro-port/src/tracking3_fishing_tracker.cpp`
     - `cpp-macro-port/include/treasure_ref_port.hpp`
     - `cpp-macro-port/src/treasure_ref_port.cpp`

## In Progress
1. None.

## Next
1. Add richer replay comparator coverage for controller-mode-specific metrics.
2. Add replay adapters for raw C# debug log formats beyond CSV.
3. Run full parity pass with captured traces and close remaining behavioral gaps.
