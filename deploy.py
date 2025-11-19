#!/usr/bin/env python3
"""
Deployment script for Ambit project
Pushes to git and uploads files via SFTP
From Windows host to Linux target.
"""

import os
import sys
import subprocess
import fnmatch
from pathlib import Path
from typing import List, Set

import paramiko

# Configuration
SFTP_USER = "abaljeu"
SFTP_HOST = "ftp.collaborative-systems.org"
SFTP_PORT = 22
REMOTE_DIR = "public_html/ambit"
SSH_RSA = Path(os.path.expanduser("~/.ssh/collab-sys.rsa"))

# Colors for output
GREEN = '\033[0;32m'
RED = '\033[0;31m'
YELLOW = '\033[1;33m'
NC = '\033[0m'  # No Color

# Ignore patterns
IGNORE_PATTERNS = [
    ".specstory",
    ".git",
    ".gitignore",
    "node_modules",
    "*.tsbuildinfo",
    "php/config.php",
    "src",
    "tsconfig.json",
    "package*.json",
    "deploy.sh",
    "deploy.py",
]


def print_colored(message: str, color: str):
    """Print colored message"""
    print(f"{color}{message}{NC}")


def should_ignore(path: str) -> bool:
    """Check if a path should be ignored based on patterns"""
    path_parts = Path(path).parts
    
    for pattern in IGNORE_PATTERNS:
        # Handle wildcard patterns
        if '*' in pattern:
            if fnmatch.fnmatch(path, pattern) or fnmatch.fnmatch(Path(path).name, pattern):
                return True
        # Handle exact matches or directory matches
        elif pattern in path_parts or path == pattern or path.startswith(pattern + '/'):
            return True
    
    return False


def git_push():
    """Push to git, handling upstream branch creation if needed"""
    print_colored("Step 1: Pushing to git...", YELLOW)
    
    result = subprocess.run(
        ["git", "push", "origin"],
        capture_output=True,
        text=True,
        check=True
    )
    if result.stdout:
        print(result.stdout)
    print_colored("Git push successful!", GREEN)
    return True


def upload_files():
    """Upload files via SFTP"""
    print_colored("Step 2: Uploading files via SFTP...", YELLOW)
    print_colored(f"Connecting to {SFTP_HOST}:{SFTP_PORT} as {SFTP_USER}", YELLOW)
    
    # Check if key file exists
    if not SSH_RSA.exists():
        print_colored(f"Error: SSH key file not found: {SSH_RSA}", RED)
        return False
    
    # Collect files to upload
    print_colored("Preparing files for upload...", YELLOW)
    files_to_upload = []
    remote_dirs_to_create: Set[str] = set()
    
    for local_path in Path('.').rglob('*'):
        if not local_path.is_file():
            continue
        
        remote_path = str(local_path.relative_to('.'))
        
        # Skip if should be ignored
        if should_ignore(remote_path):
            continue
        
        files_to_upload.append(local_path)
        # Collect parent directories
        parent = local_path.parent
        if parent != Path('.'):
            remote_dirs_to_create.add(str(parent).replace('\\', '/'))
    
    if not files_to_upload:
        print_colored("No files to upload!", YELLOW)
        return True
    
    # Connect via SFTP
    # Create SSH client
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    
    # Load private key
    import getpass
    passphrase = getpass.getpass(f"Enter passphrase for {SSH_RSA}: ")
    private_key = paramiko.RSAKey.from_private_key_file(str(SSH_RSA), password=passphrase)
    
    # Connect
    ssh.connect(
        SFTP_HOST,
        port=SFTP_PORT,
        username=SFTP_USER,
        pkey=private_key,
        look_for_keys=False
    )
    
    sftp = ssh.open_sftp()
    
    # Change to remote directory
    sftp.chdir(REMOTE_DIR)
    
    # Create directories using relative paths from REMOTE_DIR
    print_colored("Making directories", YELLOW)
    for remote_dir_path in sorted(remote_dirs_to_create):
        print('.', end='', flush=True)
        try:
            sftp.stat(remote_dir_path)
        except (IOError, FileNotFoundError):
            try:
                sftp.mkdir(remote_dir_path)
            except Exception:
                print_colored(f"Failed to create directory {remote_dir_path}", RED)
                raise
    
    # Upload files using relative paths from REMOTE_DIR
    print_colored("Uploading files", YELLOW)
    for local_path in files_to_upload:
        print('.', end='', flush=True)
        remote_path = str(local_path.relative_to('.')).replace('\\', '/')
        
        # Check if remote file exists and compare modification times
        should_upload = True
        try:
            remote_stat = sftp.stat(remote_path)
            local_mtime = local_path.stat().st_mtime
            remote_mtime = remote_stat.st_mtime
            should_upload = local_mtime > remote_mtime +1
            if should_upload:
                print(f"\nUploading {remote_path}: local={int(local_mtime)}, remote={int(remote_mtime)}, diff={int(local_mtime - remote_mtime)}")
        except (IOError, FileNotFoundError):
            should_upload = True
        
        if should_upload:
            try:
                print_colored(remote_path, YELLOW)
                sftp.put(str(local_path), remote_path)
                # Preserve local file modification time
                local_stat = local_path.stat()
                sftp.utime(remote_path, (local_stat.st_atime, local_stat.st_mtime))
            except Exception:
                print_colored(f"Failed to upload {remote_path}", RED)
    
    sftp.close()
    ssh.close()
    
    print_colored("SFTP upload successful!", GREEN)
    return True

def main():
    """Main deployment function"""
    print_colored("Starting deployment...", GREEN)
    
    # Step 1: Git push
    if not git_push():
        sys.exit(1)
    
    # Step 2: Upload files
    if not upload_files():
        sys.exit(1)
    
    print_colored("Deployment completed successfully!", GREEN)


if __name__ == "__main__":
    main()

