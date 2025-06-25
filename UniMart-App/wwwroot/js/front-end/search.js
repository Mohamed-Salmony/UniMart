function toggleSearchPopup() {
    const searchPopup = document.getElementById('searchPopup');
    if (searchPopup.classList.contains('d-none')) {
        searchPopup.classList.remove('d-none');
        document.getElementById('searchInput').focus();
    } else {
        searchPopup.classList.add('d-none');
    }
}

function searchProducts(event) {
    event.preventDefault();
    const query = document.getElementById('searchInput').value;
    if (query.trim() !== '') {
        // In ASP.NET, we'll redirect to a search results page
        window.location.href = '/Home/Search?query=' + encodeURIComponent(query);
    }
    toggleSearchPopup();
}
