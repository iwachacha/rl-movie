from __future__ import annotations

import json
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
NOTEBOOK_PATH = SCRIPT_DIR / "rl_movie_training.ipynb"
REQUIREMENTS_PATH = SCRIPT_DIR / "mlagents-training-requirements.txt"


def load_requirements_text() -> str:
    lines: list[str] = []
    for raw_line in REQUIREMENTS_PATH.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        lines.append(line)
    if not lines:
        raise ValueError(f"No requirements found in {REQUIREMENTS_PATH}")
    return "\n".join(lines)


def to_source_lines(cell_code: str) -> list[str]:
    return [f"{line}\n" for line in cell_code.splitlines()]


def build_install_cell_source(requirements_text: str) -> list[str]:
    requirements_literal = json.dumps(requirements_text)
    cell_code = f"""#@title ML-Agents のインストール {{ run: "auto" }}
import os
import subprocess
import sys
import textwrap

TRAINING_REQUIREMENTS = {requirements_literal}
TRAINING_REQUIREMENTS_PATH = "/content/rl_movie_training_requirements.txt"

with open(TRAINING_REQUIREMENTS_PATH, "w", encoding="utf-8") as stream:
    stream.write(TRAINING_REQUIREMENTS + "\\n")

print("Pinned training requirements:")
print(TRAINING_REQUIREMENTS)

def is_supported_mlagents_python(version_info):
    return version_info.major == 3 and version_info.minor == 10 and 1 <= version_info.micro <= 12

TRAINING_ENV_KIND = "host-python"
TRAINING_PYTHON_BIN = sys.executable

if not is_supported_mlagents_python(sys.version_info):
    TRAINING_ENV_KIND = "micromamba-py310"
    TRAINING_ENV_DIR = "/content/.mlagents-py310"
    TRAINING_PYTHON_BIN = f"{{TRAINING_ENV_DIR}}/bin/python"
    MICROMAMBA_BIN = "/content/bin/micromamba"

    if not os.path.exists(TRAINING_PYTHON_BIN):
        print(f"Host Python {{sys.version.split()[0]}} is incompatible with mlagents 1.1.0. Creating a Python 3.10.12 training environment...")
        subprocess.check_call([
            "bash",
            "-lc",
            "set -euo pipefail; mkdir -p /content/bin; curl -Ls https://micro.mamba.pm/api/micromamba/linux-64/latest | tar -xvj -C /content/bin bin/micromamba --strip-components=1"
        ])
        subprocess.check_call([MICROMAMBA_BIN, "create", "-y", "-p", TRAINING_ENV_DIR, "python=3.10.12", "pip"])
    else:
        print(f"Reusing Python 3.10 training environment: {{TRAINING_ENV_DIR}}")
else:
    print(f"Host Python {{sys.version.split()[0]}} is directly compatible with mlagents 1.1.0.")

subprocess.check_call([TRAINING_PYTHON_BIN, "-m", "pip", "install", "-q", "-r", TRAINING_REQUIREMENTS_PATH])

version_probe = textwrap.dedent(
    '''
    from importlib.metadata import PackageNotFoundError, version
    import sys
    import torch
    import onnx

    def safe_version(package_name):
        try:
            return version(package_name)
        except PackageNotFoundError:
            return "not-installed"

    print(f"Training Python: {{sys.executable}}")
    print(f"Training Python version: {{sys.version.split()[0]}}")
    print(f"ML-Agents installed: {{safe_version('mlagents')}}")
    print(f"ML-Agents envs installed: {{safe_version('mlagents-envs')}}")
    print(f"Protobuf installed: {{safe_version('protobuf')}}")
    print(f"Torch installed: {{torch.__version__}}")
    print(f"ONNX installed: {{onnx.__version__}}")
    print(f"CUDA available: {{torch.cuda.is_available()}}")
    '''
)

probe = subprocess.run([TRAINING_PYTHON_BIN, "-c", version_probe], capture_output=True, text=True, check=True)
print(f"Training environment: {{TRAINING_ENV_KIND}}")
print(f"Requirements file: {{TRAINING_REQUIREMENTS_PATH}}")
print(probe.stdout)
if probe.stderr.strip():
    print(probe.stderr)
"""
    return to_source_lines(cell_code)


def build_validate_cell_source() -> list[str]:
    cell_code = """#@title Validate Manifest and Run ID { run: "auto" }
import json
import yaml

if not HYPOTHESIS.strip():
    raise ValueError('HYPOTHESIS must not be empty')

if not re.fullmatch(RUN_ID_PATTERN, RUN_ID):
    raise ValueError(
        f"RUN_ID does not match the required pattern: {RUN_ID_PATTERN}\\n"
        f"Current RUN_ID: {RUN_ID}"
    )

if not os.path.exists(manifest_path):
    raise FileNotFoundError(f"scenario_manifest.yaml not found: {manifest_path}")

with open(manifest_path, 'r', encoding='utf-8') as stream:
    manifest = yaml.safe_load(stream)

required_manifest_keys = [
    'scenario_name', 'scene_name', 'agent_class', 'behavior_name', 'learning_goal',
    'success_conditions', 'failure_conditions', 'observation_contract', 'action_contract',
    'reward_rules', 'randomization_knobs', 'difficulty_stages', 'visual_theme',
    'camera_plan', 'acceptance_criteria', 'baseline_run', 'spec_version'
]
missing_keys = [key for key in required_manifest_keys if key not in manifest]
if missing_keys:
    raise KeyError(f"manifest is missing required keys: {missing_keys}")

if manifest['scenario_name'] != SCENARIO_NAME:
    raise ValueError(f"SCENARIO_NAME ({SCENARIO_NAME}) does not match manifest scenario_name ({manifest['scenario_name']})")

if str(manifest['spec_version']) != str(SPEC_VERSION):
    raise ValueError(f"SPEC_VERSION ({SPEC_VERSION}) does not match manifest spec_version ({manifest['spec_version']})")

build_requirements_path = os.path.join(CONFIG_DIR, 'training_requirements.txt')
if os.path.exists(build_requirements_path):
    with open(build_requirements_path, 'r', encoding='utf-8') as stream:
        build_requirements = '\\n'.join(
            line.strip()
            for line in stream.readlines()
            if line.strip() and not line.lstrip().startswith('#')
        )
    notebook_requirements = '\\n'.join(
        line.strip()
        for line in TRAINING_REQUIREMENTS.splitlines()
        if line.strip() and not line.lstrip().startswith('#')
    )
    if build_requirements != notebook_requirements:
        raise ValueError(
            'Build ZIP training_requirements.txt does not match notebook pinned requirements.\\n'
            f'Build file: {build_requirements_path}\\n'
            f'Notebook file: {TRAINING_REQUIREMENTS_PATH}'
        )
    print(f"Training requirements verified: {build_requirements_path}")
else:
    print('Training requirements file not found in build ZIP. Using notebook-pinned requirements.')

training_configs = [
    path for path in config_files
    if os.path.basename(path) not in {'scenario_manifest.yaml', 'training_requirements.txt'}
]
if not training_configs:
    raise FileNotFoundError('No training config YAML found next to scenario_manifest.yaml')

config_path = training_configs[0]
with open(config_path, 'r', encoding='utf-8') as stream:
    config = yaml.safe_load(stream)

behavior_names = list(config.get('behaviors', {}).keys())
if manifest['behavior_name'] not in behavior_names:
    raise ValueError(
        f"manifest behavior_name ({manifest['behavior_name']}) not found in training config behaviors ({behavior_names})"
    )

for behavior_name, behavior_config in config.get('behaviors', {}).items():
    behavior_config['max_steps'] = MAX_STEPS
    behavior_config['checkpoint_interval'] = CHECKPOINT_INTERVAL
    behavior_config['keep_checkpoints'] = KEEP_CHECKPOINTS
    behavior_config['summary_freq'] = SUMMARY_FREQ
    print(f"{behavior_name}: max_steps -> {MAX_STEPS:,}")
    print(f"{behavior_name}: checkpoint_interval -> {CHECKPOINT_INTERVAL:,}")
    print(f"{behavior_name}: keep_checkpoints -> {KEEP_CHECKPOINTS}")
    print(f"{behavior_name}: summary_freq -> {SUMMARY_FREQ:,}")

with open(config_path, 'w', encoding='utf-8') as stream:
    yaml.dump(config, stream, sort_keys=False, default_flow_style=False, allow_unicode=True)

effective_baseline_run = BASELINE_RUN.strip() or str(manifest.get('baseline_run', '')).strip()
if BASELINE_RUN.strip() and str(manifest.get('baseline_run', '')).strip() and BASELINE_RUN.strip() != str(manifest.get('baseline_run', '')).strip():
    raise ValueError('BASELINE_RUN input does not match manifest baseline_run')

run_context = {
    'scenario_name': SCENARIO_NAME,
    'scenario_slug': SCENARIO_SLUG,
    'run_id': RUN_ID,
    'spec_version': SPEC_VERSION,
    'hypothesis': HYPOTHESIS,
    'seed': SEED,
    'baseline_run': effective_baseline_run,
    'max_steps': MAX_STEPS,
    'resume_training': RESUME_TRAINING,
    'checkpoint_interval': CHECKPOINT_INTERVAL,
    'keep_checkpoints': KEEP_CHECKPOINTS,
    'summary_freq': SUMMARY_FREQ,
    'zip_path': zip_path,
    'manifest_path': manifest_path,
    'config_path': config_path,
    'behavior_names': behavior_names,
}

os.makedirs(RUN_RESULTS_DIR, exist_ok=True)
with open(RUN_CONTEXT_PATH, 'w', encoding='utf-8') as stream:
    json.dump(run_context, stream, ensure_ascii=False, indent=2)

print(json.dumps(run_context, ensure_ascii=False, indent=2))
print(f"Run context saved: {RUN_CONTEXT_PATH}")
"""
    return to_source_lines(cell_code)


def build_training_cell_source() -> list[str]:
    cell_code = """#@title Run Training { run: "auto" }
import glob
import html
import subprocess
from collections import deque
from IPython.display import HTML, display

exec_path = exec_files[0]
resume_flag = '--resume' if RESUME_TRAINING else '--force'

cmd = [
    TRAINING_PYTHON_BIN,
    '-m',
    'mlagents.trainers.learn',
    config_path,
    f'--env={exec_path}',
    f'--run-id={RUN_ID}',
    f'--seed={SEED}',
    '--no-graphics',
    f'--results-dir={DRIVE_RESULTS}',
    resume_flag,
]
cmd.extend(['--env-args', '-logFile', UNITY_PLAYER_LOG_PATH])

step_pattern = re.compile(r'Step:\\s*([0-9]+)')
reward_pattern = re.compile(r'Mean Reward:\\s*(-?[0-9]+(?:\\.[0-9]+)?)')
elapsed_pattern = re.compile(r'Time Elapsed:\\s*([0-9]+(?:\\.[0-9]+)?)\\s*s')
recent_logs = deque(maxlen=12)

def utc_now():
    return datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')

def collect_artifacts():
    run_dir = os.path.join(DRIVE_RESULTS, RUN_ID)
    checkpoints = glob.glob(f"{run_dir}/**/*.pt", recursive=True)
    onnx_models = glob.glob(f"{run_dir}/**/*.onnx", recursive=True)
    event_files = glob.glob(f"{run_dir}/**/events.out.tfevents.*", recursive=True)
    candidates = checkpoints + onnx_models + event_files
    latest_artifact = max(candidates, key=os.path.getmtime) if candidates else ''
    return {
        'checkpoint_count': len(checkpoints),
        'onnx_count': len(onnx_models),
        'event_file_count': len(event_files),
        'latest_artifact': os.path.basename(latest_artifact) if latest_artifact else '',
    }

progress = {
    'scenario_name': SCENARIO_NAME,
    'run_id': RUN_ID,
    'status': 'starting',
    'stage': 'training',
    'max_steps': MAX_STEPS,
    'current_step': 0,
    'mean_reward': None,
    'elapsed_seconds': 0.0,
    'checkpoint_count': 0,
    'onnx_count': 0,
    'event_file_count': 0,
    'latest_artifact': '',
    'recent_logs': [],
    'updated_utc': utc_now(),
}

def persist_progress():
    os.makedirs(RUN_RESULTS_DIR, exist_ok=True)
    progress['recent_logs'] = list(recent_logs)
    progress['updated_utc'] = utc_now()
    progress.update(collect_artifacts())
    with open(RUN_PROGRESS_PATH, 'w', encoding='utf-8') as stream:
        json.dump(progress, stream, ensure_ascii=False, indent=2)

def render_dashboard():
    percent = 0.0
    if MAX_STEPS:
        percent = min(progress['current_step'] / MAX_STEPS * 100.0, 100.0)

    reward_text = '-' if progress['mean_reward'] is None else f"{progress['mean_reward']:.3f}"
    logs_html = '<br>'.join(html.escape(line) for line in recent_logs) or 'Waiting for trainer output...'
    latest_artifact = progress['latest_artifact'] or '-'

    return f\"\"\"
    <div style='font-family:Arial,sans-serif;border:1px solid #d6dce5;border-radius:12px;padding:16px;background:#f7f9fc;'>
      <div style='display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;'>
        <div>
          <div style='font-size:20px;font-weight:700;color:#16324f;'>RL Movie Training Progress</div>
          <div style='font-size:13px;color:#52667a;'>Run ID: {html.escape(RUN_ID)}</div>
        </div>
        <div style='font-size:13px;color:#52667a;'>Status: <strong>{html.escape(progress['status'])}</strong></div>
      </div>
      <div style='margin-top:14px;height:14px;background:#dde6f0;border-radius:999px;overflow:hidden;'>
        <div style='width:{percent:.2f}%;height:100%;background:linear-gradient(90deg,#2e86de,#37c978);'></div>
      </div>
      <div style='margin-top:8px;font-size:12px;color:#52667a;'>{progress['current_step']:,} / {MAX_STEPS:,} steps ({percent:.1f}%)</div>
      <div style='display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:10px;margin-top:14px;'>
        <div style='background:white;border-radius:10px;padding:10px;'><div style='font-size:12px;color:#6b7c8f;'>Mean reward</div><div style='font-size:24px;font-weight:700;color:#16324f;'>{reward_text}</div></div>
        <div style='background:white;border-radius:10px;padding:10px;'><div style='font-size:12px;color:#6b7c8f;'>Elapsed</div><div style='font-size:24px;font-weight:700;color:#16324f;'>{progress['elapsed_seconds']:.1f}s</div></div>
        <div style='background:white;border-radius:10px;padding:10px;'><div style='font-size:12px;color:#6b7c8f;'>Checkpoints</div><div style='font-size:24px;font-weight:700;color:#16324f;'>{progress['checkpoint_count']}</div></div>
        <div style='background:white;border-radius:10px;padding:10px;'><div style='font-size:12px;color:#6b7c8f;'>Event files</div><div style='font-size:24px;font-weight:700;color:#16324f;'>{progress['event_file_count']}</div></div>
      </div>
      <div style='margin-top:14px;background:white;border-radius:10px;padding:10px;'>
        <div style='font-size:12px;color:#6b7c8f;'>Latest artifact</div>
        <div style='font-size:14px;font-weight:600;color:#16324f;'>{html.escape(latest_artifact)}</div>
      </div>
      <div style='margin-top:14px;background:#0f1720;color:#d9e2ec;border-radius:10px;padding:12px;'>
        <div style='font-size:12px;color:#8ea3b7;margin-bottom:8px;'>Recent trainer logs</div>
        <div style='font-family:Consolas,Monaco,monospace;font-size:12px;line-height:1.5;'>{logs_html}</div>
      </div>
      <div style='margin-top:10px;font-size:12px;color:#52667a;'>progress.json: {html.escape(RUN_PROGRESS_PATH)}</div>
    </div>
    \"\"\"

def tail_text_file(path, max_lines=40):
    if not os.path.exists(path):
        return f"[missing] {path}"
    with open(path, 'r', encoding='utf-8', errors='replace') as stream:
        lines = stream.readlines()
    tail = ''.join(lines[-max_lines:]).strip()
    return tail or f"[empty] {path}"

print('Training command:')
print(' '.join(cmd))

os.makedirs(RUN_RESULTS_DIR, exist_ok=True)
persist_progress()
display_handle = display(HTML(render_dashboard()), display_id=True)

with open(RUN_LOG_PATH, 'w', encoding='utf-8') as log_stream:
    process = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        bufsize=1,
        env={**os.environ, 'PYTHONUNBUFFERED': '1'},
    )

    for raw_line in process.stdout:
        line = raw_line.rstrip()
        if not line:
            continue

        log_stream.write(line + '\\n')
        log_stream.flush()
        recent_logs.append(line)

        step_match = step_pattern.search(line)
        reward_match = reward_pattern.search(line)
        elapsed_match = elapsed_pattern.search(line)

        if step_match:
            progress['current_step'] = int(step_match.group(1))
        if reward_match:
            progress['mean_reward'] = float(reward_match.group(1))
        if elapsed_match:
            progress['elapsed_seconds'] = float(elapsed_match.group(1))

        progress['status'] = 'running'
        persist_progress()
        display_handle.update(HTML(render_dashboard()))

    return_code = process.wait()

progress['status'] = 'completed' if return_code == 0 else f"failed ({return_code})"
persist_progress()
display_handle.update(HTML(render_dashboard()))

if return_code != 0:
    trainer_log_tail = tail_text_file(RUN_LOG_PATH)
    unity_log_tail = tail_text_file(UNITY_PLAYER_LOG_PATH)
    print("Trainer log tail:")
    print(trainer_log_tail)
    print("\\nUnity player log tail:")
    print(unity_log_tail)
    raise RuntimeError(
        f"Training failed with exit code {return_code}\\n\\n"
        f"Trainer log: {RUN_LOG_PATH}\\n"
        f"Unity player log: {UNITY_PLAYER_LOG_PATH}\\n\\n"
        f"===== Trainer log tail =====\\n{trainer_log_tail}\\n\\n"
        f"===== Unity player log tail =====\\n{unity_log_tail}"
    )

print(f"Training log saved: {RUN_LOG_PATH}")
"""
    return to_source_lines(cell_code)


def replace_cell_source(notebook: dict, predicate, new_source: list[str]) -> None:
    for cell in notebook["cells"]:
        if cell.get("cell_type") != "code":
            continue
        source = "".join(cell.get("source", []))
        if predicate(source):
            cell["source"] = new_source
            return
    raise ValueError("Target notebook cell not found")


def main() -> None:
    requirements_text = load_requirements_text()
    notebook = json.loads(NOTEBOOK_PATH.read_text(encoding="utf-8"))

    replace_cell_source(
        notebook,
        lambda source: "TRAINING_REQUIREMENTS_PATH" in source or source.startswith("#@title ML-Agents"),
        build_install_cell_source(requirements_text),
    )
    replace_cell_source(
        notebook,
        lambda source: source.startswith("#@title Validate Manifest and Run ID"),
        build_validate_cell_source(),
    )
    replace_cell_source(
        notebook,
        lambda source: source.startswith("#@title Run Training"),
        build_training_cell_source(),
    )

    NOTEBOOK_PATH.write_text(
        json.dumps(notebook, ensure_ascii=False, indent=4) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
