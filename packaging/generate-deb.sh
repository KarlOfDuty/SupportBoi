#!/usr/bin/bash -e

# I know nothing about deb packaging at the time of writing, and it seems
# to be a real mess. In case anyone who know this better than me wants to
# help, I would appreciate the feedback.

# Usage: Run this script from the root of the repository.
# Set the DEV_BUILD environment variable to true to generate a dev build instead of a release.


# Set up package version and name depending on whether this is a dev build or release
BASE_VERSION="$(sed -ne '/Version/{s/.*<Version>\(.*\)<\/Version>.*/\1/p;q;}' < SupportBoi.csproj)"
if [[ "$DEV_BUILD" == "false" ]]; then
  PACKAGE_VERSION="$BASE_VERSION"
  PACKAGE_NAME="supportboi"
else
  PACKAGE_VERSION="$BASE_VERSION~$(date "+%Y%m%d%H%M%S")git$(git rev-parse --short HEAD)"
  PACKAGE_NAME="supportboi-dev"
fi

export REPO_ROOT="$PWD"

# Check what the package build dir should be called, mostly exists so parallel CI jobs don't clash
if [[ -z "$PACKAGE_ROOT" ]]; then
  PACKAGE_ROOT="$REPO_ROOT/.dpkg-deb"
fi

# Export various environment variables so the packaging scripts can use them
export PACKAGE_VERSION
export PACKAGE_NAME
export PACKAGE_ROOT

export FULL_PACKAGE_NAME="${PACKAGE_NAME}_$PACKAGE_VERSION"
export PACKAGE_DIR="$PACKAGE_ROOT/$FULL_PACKAGE_NAME"

# Remove old package build dir if it exists
rm -rf "$PACKAGE_ROOT"

# Create source code tarball as the debian packaging system likes to have one
git archive --format=tar.gz HEAD > "${FULL_PACKAGE_NAME}.orig.tar.gz"

# Create the package build directory and extract the source code into it
mkdir -p "$PACKAGE_DIR"
cd "$PACKAGE_DIR" || exit 1
mv "$REPO_ROOT/$FULL_PACKAGE_NAME.orig.tar.gz" "$PACKAGE_ROOT/"
tar -xzf "$PACKAGE_ROOT/$FULL_PACKAGE_NAME.orig.tar.gz"

# Copy the debian package files into the package build directory and replace variables
cp -r "$REPO_ROOT/packaging/debian" "$PACKAGE_DIR/"
sed -i 's/PACKAGE_NAME/'"$PACKAGE_NAME"'/' "$PACKAGE_DIR/debian/control"
sed -i 's/PACKAGE_NAME/'"$PACKAGE_NAME"'/' "$PACKAGE_DIR/debian/changelog"
sed -i 's/PACKAGE_VERSION/'"$PACKAGE_VERSION"'/' "$PACKAGE_DIR/debian/changelog"

if [[ -z "$DEV_BUILD" ]]; then
  sed -i 's/DIST/'"release"'/' "$PACKAGE_DIR/debian/changelog"
else
  sed -i 's/DIST/'"dev"'/' "$PACKAGE_DIR/debian/changelog"
fi

# Set packager name and email if not explicitly set
if [[ -z "$DEBEMAIL" || -z "$DEBEMAIL" ]]; then
  echo -e "You must set DEBFULLNAME and DEBEMAIL. Example:\nexport DEBFULLNAME=\"Karl Essinger\"\nexport DEBEMAIL=\"xkaess22@gmail.com\""
  exit 1
fi

# Build the .deb package
dpkg-buildpackage -us -uc