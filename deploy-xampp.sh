#!/bin/bash
# Deploy Ambit to XAMPP

echo "Deploying Ambit to XAMPP..."

# Compile TypeScript
echo "Compiling TypeScript..."
tsc
if [ $? -ne 0 ]; then
    echo "TypeScript compilation failed!"
    exit 1
fi

# Set paths
SOURCE="."
DEST="/d/xampp/htdocs/ambit"

# Create destination if it doesn't exist
mkdir -p "$DEST"

# Copy files
echo "Copying files to $DEST..."
cp -rf "$SOURCE/dist" "$DEST/dist"
cp -rf "$SOURCE/php/"* "$DEST/"

# Create doc directory if it exists
if [ -d "$SOURCE/doc" ]; then
    cp -rf "$SOURCE/doc" "$DEST/"
fi

echo ""
echo "Deployment complete!"
echo "Access your app at: http://localhost/ambit/ambit.php?doc=notes.txt"
echo ""








