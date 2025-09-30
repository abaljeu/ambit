import argparse
import subprocess
import zipfile
import shutil
from pathlib import Path
from datetime import datetime

class CodebaseSwitcher:

    def __init__(self):
        self.states = ['preedit', 'gemini', 'sonnet']
        self.current_dir = Path.cwd()
        self.script_files = ['switch.py']

    def add_to_ignore(self, ignore_file, entry, entry_comment):

        try:
            ignore_path = self.current_dir / ignore_file
            
            if ignore_path.exists():
                existing_content = ignore_path.read_text(encoding='utf-8')
                if entry not in existing_content.strip().split('\n'):
                    new_content = existing_content.rstrip() + "\n\n" + entry_comment + "\n" + entry + "\n"
                    ignore_path.write_text(new_content, encoding='utf-8')
                    print(f"Updated {ignore_file} to exclude {entry}")
                else:
                    print(f"{ignore_file} already excludes {entry}")
            else:
                ignore_path.write_text(
                    entry_comment + "\n" + entry + "\n",
                    encoding='utf-8'
                )
                print(f"Created {ignore_file} to exclude {entry}")
        
        except (OSError, UnicodeError) as e:
            print(f"⚠️  Warning: Could not create/update {ignore_file}: {e}")

    def run_git_command(self, args, capture_output=False):
        """Run git command with proper cross-platform handling"""

        cmd = ['git'] + (args if isinstance(args, list) else args.split())
        result = subprocess.run(cmd, capture_output=capture_output, text=True, 
                                check=not capture_output, cwd=self.current_dir)

        if capture_output:
            if result.returncode == 0:
                return result.stdout.strip()
            else:
                print(f"⚠️  Error running git command: {result.stderr.strip()}")
                return ""

        return result.returncode == 0

    
    def check_git_available(self):
        """Check if git is available and print helpful error if not"""
        if not shutil.which('git'):
            print("❌ Git not found in PATH.")
            return False
        return True
    
    def get_current_branch(self):
        return self.run_git_command(['branch', '--show-current'], capture_output=True) or "main"
    
    def branch_exists(self, branch):
        result = self.run_git_command(['branch', '--list', branch], capture_output=True)
        return branch in result if result else False
    
    def initialize(self):
        """Initialize git repo with states"""
        if not self.check_git_available():
            raise RuntimeError("Git must be installed and available for this script to run.")



        self.add_to_ignore('.cursorignore', "codebase_switcher.py", "# Ignore codebase switcher script to avoid contaminating AI conversations\n")
        self.add_to_ignore('.gitignore', "codebase_switcher.py", "# Ignore codebase switcher script\n")
        self.add_to_ignore('.gitignore', ".cursorignore", "# Ignore .cursorignore files\n")
        self.add_to_ignore('.gitignore', ".cursorindexingignore", "# Ignore cursor indexing ignore script\n")
        self.add_to_ignore('.gitignore', ".specstory/", "# Ignore specstory folders.\n")

        if not (self.current_dir / '.git').exists():
            print("🔧 Initializing git repository...")
            self.run_git_command(['init'])

            self.run_git_command(['config', '--local', 'user.name', 'Codebase Switcher'])
            self.run_git_command(['config', '--local', 'user.email', 'switcher@codebase.local'])
            
            self.run_git_command(['add', '.'])
            self.run_git_command(['commit', '-m', 'Initial commit'])

        elif self.run_git_command(['diff'], capture_output=True):
            self.run_git_command(['add', '.'])

        # if init was run more than once there might not be any changes to commit
        if self.run_git_command(['diff', '--staged'], capture_output=True):
            self.run_git_command(['commit', '-m', 'Commit .gitignore changes'])

        if self.get_current_branch() != "preedit":
            if self.branch_exists('preedit'):
                self.run_git_command(['branch', '-D', 'preedit'])
            self.run_git_command(['checkout', '-b', 'preedit'])

        # Create gemini and sonnet branches from preedit
        for model in ['gemini', 'sonnet']:
            if self.branch_exists(model):
                self.run_git_command(['branch', '-D', model])            

            print(f"📝 Creating {model} branch from preedit")
            self.run_git_command(['checkout', '-b', model])
        
        self.run_git_command(['checkout', 'preedit'])
            
        print(f"✅ Initialized with states: {', '.join(self.states)}")
        print("📍 Currently on: preedit")
        return True
    
    def switch_state(self, state):
        """Switch to specified state"""
        if state not in ['gemini', 'sonnet']:
            raise ValueError(f"❌ Invalid state {state}. Available: gemini, sonnet")
        
        if not self.check_git_available():
            raise RuntimeError("Git must be installed and available for this script to run.")
        
        if not (self.current_dir / '.git').exists():
            raise RuntimeError("❌ Not a git repo. Run this script with --init first..")
        
        if not self.branch_exists(state):
            raise RuntimeError(f"❌ State '{state}' doesn't exist. Run --init first.")
        
        current = self.get_current_branch()
        if current == state:
            print(f"✅ Already on {state}")
            return True
        
        # Handle changes based on transition type
        if self.run_git_command(['status', '--porcelain'], capture_output=True):
            if current == 'preedit' and state == 'gemini':
                # Discard changes when moving from preedit to gemini (preserve original state)
                print(f"🗑️  Discarding changes from {current} to preserve original state")
                self.run_git_command(['reset', '--hard'])
            else:
                # Auto-commit changes for all other transitions
                print(f"💾 Auto-committing changes from {current} before switching to {state}")
                self.run_git_command(['add', '.'])
                self.run_git_command(['commit', '-m', f'Auto-commit from {current} before switching to {state}'])
        
        print(f"🔄 Switching to {state}...")
        if self.run_git_command(['checkout', state]):
            print(f"✅ Now on {state}")
            return True
        
        raise RuntimeError(f"❌ Failed to switch to {state}")

    
    def show_status(self):
        """Show current status"""
        if not self.check_git_available():
            return
        
        if not (self.current_dir / '.git').exists():
            print("❌ Not a git repo. Run --init first.")
            return
        
        print(f"📍 Current state: {self.get_current_branch()}")
        print(f"📋 Available: gemini, sonnet")
        print(f"\n🔄 Git status:")
        if not self.run_git_command(['status', '--short']):
            print("No changes")
        print(f"\n📚 Recent commits:")
        if not self.run_git_command(['log', '--oneline', '-3']):
            print("No commits found")
    
    def cleanup_switcher_branches(self):
        """Remove switcher branches, keep files"""
        print("🧹 Cleaning up switcher branches...")
        
        current = self.get_current_branch()
        
        safe_branch = next((b for b in ['main', 'master'] if self.branch_exists(b)), None)
        
        if not safe_branch:
            print("🔄 Creating main branch...")
            if not self.run_git_command(['checkout', '-b', 'main']):
                print("❌ Failed to create main branch")
                return False
        elif current in self.states:
            print(f"🔄 Switching to {safe_branch}...")
            if not self.run_git_command(['checkout', safe_branch]):
                print(f"❌ Failed to switch to {safe_branch}")
                return False
        
        # Delete switcher branches
        for state in self.states:
            if self.branch_exists(state):
                print(f"🗑️  Deleting {state} branch...")
                self.run_git_command(['branch', '-D', state])
        
        print("✅ Cleanup complete - switcher branches removed, files kept")
        return True
    
    def verify_branches_different(self):
        """Verify that preedit, gemini, and sonnet branches are different"""
        print("🔍 Verifying branches are different...")
        
        for state in self.states:
            if not self.branch_exists(state):
                print(f"❌ Branch '{state}' doesn't exist. Run --init first.")
                return False
        
        branch_pairs = [
            ('preedit', 'gemini'),
            ('preedit', 'sonnet'), 
            ('gemini', 'sonnet')
        ]
        
        identical_pairs = []
        for branch1, branch2 in branch_pairs:
            try:
                result = subprocess.run(['git', 'diff', '--quiet', branch1, branch2], 
                                      cwd=self.current_dir, capture_output=True, check=False)
                if result.returncode == 0:
                    identical_pairs.append((branch1, branch2))
            except FileNotFoundError:
                print("❌ Git not found")
                return False
        
        if identical_pairs:
            print("❌ Some branches are identical:")
            for branch1, branch2 in identical_pairs:
                print(f"   - {branch1} and {branch2} have identical content")
            return False
        
        print("✅ All branches are different - proceeding with zip creation")
        return True
    
    def create_zip(self):
        """Create project zip with git structure"""
        if not self.check_git_available():
            return False
        
        if not (self.current_dir / '.git').exists():
            print("❌ Not a git repo. Run --init first.")
            return False
        
        # Auto-commit any changes before creating zip
        if self.run_git_command(['status', '--porcelain'], capture_output=True):
            current = self.get_current_branch()
            print(f"💾 Auto-committing changes on {current} before creating zip")
            if not self.run_git_command(['add', '.']):
                print("❌ Failed to stage changes")
                return False
            if not self.run_git_command(['commit', '-m', f'Auto-commit on {current} before zip creation']):
                print("❌ Failed to commit changes")
                return False
        
        if not self.verify_branches_different():
            return False
            
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        zip_name = f"codebase_{self.current_dir.name}_{timestamp}.zip"
        zip_path = self.current_dir / zip_name
        
        print(f"📦 Creating {zip_name} with git structure...")
        
        try:
            with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
                for file_path in self.current_dir.rglob('*'):
                    if file_path.is_file() and file_path.name not in self.script_files and file_path.name != zip_name:
                        try:
                            arc_path = file_path.relative_to(self.current_dir).as_posix()
                            zipf.write(file_path, arc_path)
                        except (OSError, ValueError) as e:
                            print(f"⚠️  Skipping {file_path.name}: {e}")
                            continue
            
            file_size_kb = zip_path.stat().st_size / 1024
            print(f"✅ Created {zip_name} ({file_size_kb:.1f} KB)")
            print("📋 Zip contains: codebase + .git structure")
            
            self.cleanup_switcher_branches()
            return True
            
        except Exception as e:
            print(f"❌ Error creating zip: {e}")
            if zip_path.exists():
                try:
                    zip_path.unlink()
                    print("🧹 Cleaned up incomplete zip file")
                except OSError:
                    pass
            return False

def main():
    parser = argparse.ArgumentParser(description='Codebase State Switcher')
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('-i', '--init', action='store_true', help='Initialize states')
    group.add_argument('-2', '--gemini', action='store_true', help='Switch to gemini') 
    group.add_argument('-3', '--sonnet', action='store_true', help='Switch to sonnet')
    group.add_argument('-s', '--status', action='store_true', help='Show status')
    group.add_argument('-z', '--zip', action='store_true', help='Create zip')
    
    args = parser.parse_args()
    switcher = CodebaseSwitcher()
    
    if args.init:
        switcher.initialize()
    elif args.gemini:
        switcher.switch_state('gemini')
    elif args.sonnet:
        switcher.switch_state('sonnet')
    elif args.status:
        switcher.show_status()
    elif args.zip:
        switcher.create_zip()

if __name__ == '__main__':
    main()
