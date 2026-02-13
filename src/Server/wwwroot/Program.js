

export const app = document.getElementById("app");

app.textContent = "Hello from Gambol (Fable client)!";

fetch("/api/hello").then(r => r.json()).then((data) => {
    const msg = data.message;
    app.textContent = ("Server says: " + msg);
});

