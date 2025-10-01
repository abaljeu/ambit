<!-- Fixed Logout Button Component -->
<style>
    .logout-button-fixed {
        position: fixed;
        bottom: 20px;
        right: 20px;
        z-index: 9999;
        padding: 10px 20px;
        background-color: #dc3545;
        color: white;
        text-decoration: none;
        border-radius: 6px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        font-family: Arial, sans-serif;
        font-size: 14px;
        font-weight: 500;
        transition: background-color 0.2s ease, box-shadow 0.2s ease;
        display: inline-block;
    }
    .logout-button-fixed:hover {
        background-color: #c82333;
        box-shadow: 0 4px 12px rgba(0,0,0,0.3);
        text-decoration: none;
    }
    .logout-button-fixed:active {
        background-color: #bd2130;
        box-shadow: 0 2px 4px rgba(0,0,0,0.2);
    }
</style>
<a href="logout.php" class="logout-button-fixed">Logout</a>

