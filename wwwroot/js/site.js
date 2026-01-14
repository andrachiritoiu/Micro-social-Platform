// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.getElementById("globalSearchInput");
    const resultsContainer = document.getElementById("globalSearchResults");

    if (searchInput && resultsContainer) {
        searchInput.addEventListener("input", function () {
            const query = searchInput.value;

            if (query.length < 1) {
                resultsContainer.style.display = "none";
                resultsContainer.innerHTML = "";
                return;
            }

            fetch(`/Users/SearchUsers?term=${encodeURIComponent(query)}`)
                .then(response => response.json())
                .then(data => {
                    resultsContainer.innerHTML = "";
                    if (data.length > 0) {
                        data.forEach(user => {
                            const item = document.createElement("a");
                            item.href = `/Users/Show/${user.id}`;
                            item.className = "list-group-item list-group-item-action border-0 d-flex align-items-center gap-2";

                            // Avatar logic
                            let avatarHtml = '';
                            if (user.profileImage) {
                                avatarHtml = `<img src="${user.profileImage}" class="rounded-circle" width="30" height="30" style="object-fit:cover;">`;
                            } else {
                                const initial = user.userName ? user.userName.charAt(0).toUpperCase() : 'U';
                                avatarHtml = `<div class="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold" 
                                              style="width: 30px; height: 30px; background-color: #7952b3; font-size: 0.8rem;">${initial}</div>`;
                            }

                            item.innerHTML = `
                                ${avatarHtml}
                                <span>${user.userName}</span>
                            `;
                            resultsContainer.appendChild(item);
                        });
                        resultsContainer.style.display = "block";
                    } else {
                        resultsContainer.style.display = "none";
                    }
                })
                .catch(error => console.error("Error searching:", error));
        });

        // Ascunde rezultatele când dai click în afara zonei
        document.addEventListener("click", function (e) {
            if (!searchInput.contains(e.target) && !resultsContainer.contains(e.target)) {
                resultsContainer.style.display = "none";
            }
        });
    }
});
