<?php
require_once('auth.php'); // Require authentication

// Set the directory path
$directory = 'doc/';

// Open the directory
if ($handle = opendir($directory)) {
    echo '<h1>Available Documents in '.$directory.'</h1>';
    echo '<p><a href="logout.php">Logout</a></p>';
    echo '<ul>';
    // Loop through the files in the directory
    while (false !== ($file = readdir($handle))) {
        // Check if the file is a .amb file
        if (pathinfo($file, PATHINFO_EXTENSION) === 'amb') {
            // Create a link to ambit.php with the file name
            $fileName = pathinfo($file, PATHINFO_FILENAME); // Get the file name without extension
            echo '<li><a href="ambit.php?doc=' . $fileName . '.amb">' . htmlspecialchars($file) . '</a></li>';
        }
    }

    echo '</ul>';
    closedir($handle);
} else {
    echo 'Could not open the directory.';
}
?>
