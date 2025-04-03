// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get("code");

    if (code) {
        fetch(`/api/auth/signin-google?code=${code}`)
            .then(response => response.json())
            .then(data => {
                if (data.Token) {
                    localStorage.setItem("jwt", data.Token);
                    window.location.href = "/Home/Index";
                } else {
                    console.error("Login failed:", data);
                }
            })
            .catch(error => console.error("Error:", error));
    }
});
