cmake_minimum_required(VERSION 3.20)
project(cpp_macro_port LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(macro_rod
    src/rod_classifier.cpp
    src/rod_profile.cpp
    src/fishing_hold_gate.cpp
    src/hold_applier.cpp
    src/automation_input_gate.cpp
    src/auto_totem_boundary.cpp
    src/auto_totem_workflow_state.cpp
    src/replay_runner.cpp
    src/replay_trace_io.cpp
    src/parity_runner.cpp
    src/bellona_side_assigner.cpp
    src/fishing_runtime_context.cpp
    src/runtime_tracker_engine.cpp
    src/bellona_dual_controller.cpp
    src/win32_roblox_memory.cpp
    src/tracker_orchestrator.cpp
    src/tracking1_controller.cpp
    src/tracking2_controller.cpp
    src/tracking3_controller.cpp
    src/tracking1_fishing_tracker.cpp
    src/tracking2_fishing_tracker.cpp
    src/tracking3_fishing_tracker.cpp
    src/treasure_ref_port.cpp
    src/local_offsets_source.cpp
    src/offsets_source_provider.cpp
    src/world_status_reader.cpp
    src/hotbar_slot_settings.cpp
    src/hotbar_rod_resolver.cpp
    src/hotbar_rod_reader.cpp
    src/enchant_catalog.cpp
    src/enchant_colors.cpp
    src/enchant_detector.cpp
    src/hunt_detect_core.cpp
    src/appraise_colors.cpp
    src/tranquility_controller.cpp
    src/native_mouse.cpp
    src/native_keyboard.cpp
    src/auto_sovereign_recharge_settings.cpp
    src/auto_sovereign_recharge_runner.cpp
    src/aquarium_sequence_runner.cpp
    src/auto_angler_runner.cpp
    src/auto_enchant_runner.cpp
    src/tracking2_appraise_runner.cpp
    src/bellona_debug_overlay_service.cpp
)
target_include_directories(macro_rod PUBLIC include)

add_executable(openmacro_core_ipc
    src/ipc/openmacro_core_ipc.cpp
)
target_link_libraries(openmacro_core_ipc PRIVATE macro_rod)

add_executable(rod_classifier_test
    tests/rod_classifier_test.cpp
)
target_link_libraries(rod_classifier_test PRIVATE macro_rod)

add_executable(rod_profile_test
    tests/rod_profile_test.cpp
)
target_link_libraries(rod_profile_test PRIVATE macro_rod)

add_executable(fishing_hold_gate_test
    tests/fishing_hold_gate_test.cpp
)
target_link_libraries(fishing_hold_gate_test PRIVATE macro_rod)

add_executable(tracking1_controller_test
    tests/tracking1_controller_test.cpp
)
target_link_libraries(tracking1_controller_test PRIVATE macro_rod)

add_executable(tracking2_controller_test
    tests/tracking2_controller_test.cpp
)
target_link_libraries(tracking2_controller_test PRIVATE macro_rod)

add_executable(tracking3_controller_test
    tests/tracking3_controller_test.cpp
)
target_link_libraries(tracking3_controller_test PRIVATE macro_rod)

add_executable(hold_applier_test
    tests/hold_applier_test.cpp
)
target_link_libraries(hold_applier_test PRIVATE macro_rod)

add_executable(fishing_runtime_context_test
    tests/fishing_runtime_context_test.cpp
)
target_link_libraries(fishing_runtime_context_test PRIVATE macro_rod)

add_executable(tracker_orchestrator_test
    tests/tracker_orchestrator_test.cpp
)
target_link_libraries(tracker_orchestrator_test PRIVATE macro_rod)

add_executable(win32_roblox_memory_offsets_test
    tests/win32_roblox_memory_offsets_test.cpp
)
target_link_libraries(win32_roblox_memory_offsets_test PRIVATE macro_rod)

add_executable(local_offsets_source_test
    tests/local_offsets_source_test.cpp
)
target_link_libraries(local_offsets_source_test PRIVATE macro_rod)

add_executable(runtime_tracker_engine_test
    tests/runtime_tracker_engine_test.cpp
)
target_link_libraries(runtime_tracker_engine_test PRIVATE macro_rod)

add_executable(bellona_runtime_engine_test
    tests/bellona_runtime_engine_test.cpp
)
target_link_libraries(bellona_runtime_engine_test PRIVATE macro_rod)

add_executable(runtime_tracker_modes_test
    tests/runtime_tracker_modes_test.cpp
)
target_link_libraries(runtime_tracker_modes_test PRIVATE macro_rod)

add_executable(runtime_startup_assist_test
    tests/runtime_startup_assist_test.cpp
)
target_link_libraries(runtime_startup_assist_test PRIVATE macro_rod)

add_executable(tracker_orchestrator_perfect_cast_test
    tests/tracker_orchestrator_perfect_cast_test.cpp
)
target_link_libraries(tracker_orchestrator_perfect_cast_test PRIVATE macro_rod)

add_executable(runtime_perfect_cast_cache_test
    tests/runtime_perfect_cast_cache_test.cpp
)
target_link_libraries(runtime_perfect_cast_cache_test PRIVATE macro_rod)

add_executable(auto_totem_boundary_test
    tests/auto_totem_boundary_test.cpp
)
target_link_libraries(auto_totem_boundary_test PRIVATE macro_rod)

add_executable(runtime_automation_gate_test
    tests/runtime_automation_gate_test.cpp
)
target_link_libraries(runtime_automation_gate_test PRIVATE macro_rod)

add_executable(auto_totem_workflow_state_test
    tests/auto_totem_workflow_state_test.cpp
)
target_link_libraries(auto_totem_workflow_state_test PRIVATE macro_rod)

add_executable(runtime_auto_totem_workflow_integration_test
    tests/runtime_auto_totem_workflow_integration_test.cpp
)
target_link_libraries(runtime_auto_totem_workflow_integration_test PRIVATE macro_rod)

add_executable(runtime_auto_totem_executor_test
    tests/runtime_auto_totem_executor_test.cpp
)
target_link_libraries(runtime_auto_totem_executor_test PRIVATE macro_rod)

add_executable(automation_input_gate_test
    tests/automation_input_gate_test.cpp
)
target_link_libraries(automation_input_gate_test PRIVATE macro_rod)

add_executable(runtime_auto_totem_gate_block_test
    tests/runtime_auto_totem_gate_block_test.cpp
)
target_link_libraries(runtime_auto_totem_gate_block_test PRIVATE macro_rod)

add_executable(replay_runner_test
    tests/replay_runner_test.cpp
)
target_link_libraries(replay_runner_test PRIVATE macro_rod)

add_executable(replay_trace_io_test
    tests/replay_trace_io_test.cpp
)
target_link_libraries(replay_trace_io_test PRIVATE macro_rod)

add_executable(parity_runner_test
    tests/parity_runner_test.cpp
)
target_link_libraries(parity_runner_test PRIVATE macro_rod)

add_executable(bellona_side_assigner_test
    tests/bellona_side_assigner_test.cpp
)
target_link_libraries(bellona_side_assigner_test PRIVATE macro_rod)

add_executable(auto_angler_runner_test
    tests/auto_angler_runner_test.cpp
)
target_link_libraries(auto_angler_runner_test PRIVATE macro_rod)

add_executable(auto_enchant_runner_test
    tests/auto_enchant_runner_test.cpp
)
target_link_libraries(auto_enchant_runner_test PRIVATE macro_rod)

add_executable(auto_sovereign_recharge_runner_test
    tests/auto_sovereign_recharge_runner_test.cpp
)
target_link_libraries(auto_sovereign_recharge_runner_test PRIVATE macro_rod)

add_executable(enchant_detector_test
    tests/enchant_detector_test.cpp
)
target_link_libraries(enchant_detector_test PRIVATE macro_rod)

add_executable(enchant_colors_test
    tests/enchant_colors_test.cpp
)
target_link_libraries(enchant_colors_test PRIVATE macro_rod)

add_executable(appraise_colors_test
    tests/appraise_colors_test.cpp
)
target_link_libraries(appraise_colors_test PRIVATE macro_rod)

add_executable(hunt_detect_core_test
    tests/hunt_detect_core_test.cpp
)
target_link_libraries(hunt_detect_core_test PRIVATE macro_rod)

add_executable(treasure_ref_port_test
    tests/treasure_ref_port_test.cpp
)
target_link_libraries(treasure_ref_port_test PRIVATE macro_rod)

add_executable(tracking2_appraise_runner_test
    tests/tracking2_appraise_runner_test.cpp
)
target_link_libraries(tracking2_appraise_runner_test PRIVATE macro_rod)

add_executable(aquarium_sequence_runner_test
    tests/aquarium_sequence_runner_test.cpp
)
target_link_libraries(aquarium_sequence_runner_test PRIVATE macro_rod)

option(OPENMACRO_BUILD_QT_UI "Build the Qt Quick/QML desktop UI" ON)
if (OPENMACRO_BUILD_QT_UI)
    add_subdirectory(ui)
endif()
