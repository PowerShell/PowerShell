#!/bin/sh

set -eu

unset CDPATH
REPO_ROOT=$(cd -- "$(dirname -- "$0")/../.." && pwd)
SCRIPT_PATH="$REPO_ROOT/docker/InstallTarballPackage.sh"
POWERSHELL_VERSION="7.6.2"
POWERSHELL_PACKAGE="powershell-7.6.2-linux-x64.tar.gz"
PACKAGE_CONTENT="fake powershell tarball content"

# Print the SHA-256 hash for the string passed as the first argument.
get_string_sha256() {
    if command -v sha256sum >/dev/null 2>&1; then
        printf '%s' "$1" | sha256sum | awk '{ print $1 }'
    elif command -v shasum >/dev/null 2>&1; then
        printf '%s' "$1" | shasum -a 256 | awk '{ print $1 }'
    else
        echo "sha256sum or shasum is required to run these tests." >&2
        exit 1
    fi
}

PACKAGE_HASH=$(get_string_sha256 "$PACKAGE_CONTENT")

# Fail the test run with a readable message.
fail() {
    echo "FAIL: $1" >&2
    exit 1
}

# Assert that two strings are equal.
assert_eq() {
    actual=$1
    expected=$2
    message=$3

    if [ "$actual" != "$expected" ]; then
        fail "$message: expected '$expected', got '$actual'"
    fi
}

# Create fake curl and tar commands so tests avoid network and system install paths.
create_fake_commands() {
    fake_bin=$1

    mkdir -p "$fake_bin"
    cat > "$fake_bin/curl" <<EOF_CURL
#!/bin/sh
out=
url=
while [ "\$#" -gt 0 ]; do
    if [ "\$1" = "-o" ]; then
        shift
        out=\$1
    else
        url=\$1
    fi
    shift || exit 0
done

case "\$url" in
    *hashes.sha256)
        printf '%s *%s\r\n' '$PACKAGE_HASH' '$POWERSHELL_PACKAGE' > "\$out"
        ;;
    *)
        printf '%s' '$PACKAGE_CONTENT' > "\$out"
        ;;
esac
EOF_CURL

    cat > "$fake_bin/tar" <<'EOF_TAR'
#!/bin/sh
destination=
while [ "$#" -gt 0 ]; do
    if [ "$1" = "-C" ]; then
        shift
        destination=$1
    fi
    shift || exit 0
done

if [ -z "$destination" ]; then
    echo "missing tar destination" >&2
    exit 1
fi

mkdir -p "$destination"
printf '#!/bin/sh\n' > "$destination/pwsh"
EOF_TAR

    chmod +x "$fake_bin/curl" "$fake_bin/tar"
}

# Run the installer with temporary install, link, and shells paths.
run_installer() {
    work_dir=$1
    fake_bin="$work_dir/fake-bin"
    install_root="$work_dir/install-root"
    link_file="$work_dir/pwsh"
    shells_file="$work_dir/shells"

    create_fake_commands "$fake_bin"

    POWERSHELL_INSTALL_ROOT=$install_root \
    POWERSHELL_LINKFILE=$link_file \
    SHELLS_FILE=$shells_file \
    PATH="$fake_bin:$PATH" \
        sh "$SCRIPT_PATH" "$POWERSHELL_VERSION" "$POWERSHELL_PACKAGE"
}

test_creates_symlink_when_missing() {
    work_dir=$(mktemp -d)
    run_installer "$work_dir"

    expected_target="$work_dir/install-root/$POWERSHELL_VERSION/pwsh"
    [ -L "$work_dir/pwsh" ] || fail "expected pwsh link to be created"
    assert_eq "$(readlink "$work_dir/pwsh")" "$expected_target" "new link target"
}

test_keeps_matching_symlink() {
    work_dir=$(mktemp -d)
    target="$work_dir/install-root/$POWERSHELL_VERSION/pwsh"
    mkdir -p "$(dirname "$target")"
    printf '#!/bin/sh\n' > "$target"
    ln -s "$target" "$work_dir/pwsh"

    run_installer "$work_dir"

    assert_eq "$(readlink "$work_dir/pwsh")" "$target" "existing matching link target"
}

test_replaces_different_symlink() {
    work_dir=$(mktemp -d)
    old_target="$work_dir/old-pwsh"
    printf '#!/bin/sh\n' > "$old_target"
    ln -s "$old_target" "$work_dir/pwsh"

    run_installer "$work_dir"

    expected_target="$work_dir/install-root/$POWERSHELL_VERSION/pwsh"
    assert_eq "$(readlink "$work_dir/pwsh")" "$expected_target" "replaced link target"
}

test_refuses_regular_file() {
    work_dir=$(mktemp -d)
    printf 'do not replace\n' > "$work_dir/pwsh"

    if run_installer "$work_dir" > "$work_dir/stdout" 2> "$work_dir/stderr"; then
        fail "installer succeeded with a regular file at the link path"
    fi

    [ ! -L "$work_dir/pwsh" ] || fail "regular file was replaced with a symlink"
    assert_eq "$(cat "$work_dir/pwsh")" "do not replace" "regular file contents"
    grep -q 'already exists and is not a symbolic link' "$work_dir/stderr" || fail "missing regular-file refusal message"
}

test_creates_symlink_when_missing
test_keeps_matching_symlink
test_replaces_different_symlink
test_refuses_regular_file

echo "InstallTarballPackage symlink tests passed"
