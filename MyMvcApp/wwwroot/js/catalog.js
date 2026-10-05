(function () {
    'use strict';

    const root = document.getElementById('catalog');
    if (!root) return;

    const endpoint = root.dataset.endpoint;
    const errorBox = document.getElementById('catalog-error');
    const template = document.getElementById('product-card-template');
    const form = document.getElementById('add-product-form');
    const filtersBox = document.querySelector('.filters');

    let activeCategory = null;

    function showError(msg) {
        errorBox.textContent = msg;
        errorBox.classList.remove('d-none');
    }

    function clearError() {
        errorBox.textContent = '';
        errorBox.classList.add('d-none');
    }

    function renderFilters(categories, active) {
        filtersBox.innerHTML = '';

        const makeBtn = (value, label) => {
            const b = document.createElement('button');
            b.type = 'button';
            b.className = 'btn btn-outline-primary filter-btn me-1 mb-1';
            if ((value ?? '') === (active ?? '')) b.classList.add('active');
            b.textContent = label;
            b.addEventListener('click', () => load(value));
            return b;
        };

        filtersBox.appendChild(makeBtn(null, 'Все'));
        categories.forEach(c => filtersBox.appendChild(makeBtn(c, c)));
    }

    function render(products) {
        if (!products.length) {
            root.innerHTML = '<p class="text-muted">Ничего не найдено.</p>';
            return;
        }

        const frag = document.createDocumentFragment();
        products.forEach(p => {
            const node = template.content.cloneNode(true);
            node.querySelector('[data-field="name"]').textContent = p.name;
            node.querySelector('[data-field="category"]').textContent = p.category;
            node.querySelector('[data-field="price"]').textContent =
                new Intl.NumberFormat('ru-RU').format(p.price);

            node.querySelector('.card').dataset.id = p.id;

            frag.appendChild(node);
        });

        root.innerHTML = '';
        root.appendChild(frag);
    }

    async function load(category) {
        activeCategory = category ?? null;
        clearError();
        root.innerHTML = `<p class="text-muted">${root.dataset.loadingText}</p>`;

        const url = category
            ? `${endpoint}?category=${encodeURIComponent(category)}`
            : endpoint;

        try {
            const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const products = await res.json();
            render(products);

            // Обновляем кнопки фильтров только когда показываем всё
            if (!category) {
                const cats = [...new Set(products.map(p => p.category))].sort();
                renderFilters(cats, null);
            }
        } catch (err) {
            console.error(err);
            showError('Не удалось загрузить каталог: ' + err.message);
        }
    }

    // ---------- CREATE ----------
    if (form) {
        form.addEventListener('submit', async e => {
            e.preventDefault();
            clearError();

            const fd = new FormData(form);
            const body = {
                name: fd.get('name'),
                category: fd.get('category'),
                price: parseFloat(fd.get('price'))
            };

            try {
                const res = await fetch(endpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify(body)
                });

                if (!res.ok) {
                    const problem = await res.json().catch(() => ({}));
                    const msg = problem.title
                        || (problem.errors && Object.values(problem.errors).flat().join('; '))
                        || `HTTP ${res.status}`;
                    throw new Error(msg);
                }

                form.reset();
                await load(null);
            } catch (err) {
                showError('Не удалось добавить товар: ' + err.message);
            }
        });
    }

    // ---------- UPDATE / DELETE (делегирование) ----------
    root.addEventListener('click', async e => {
        const card = e.target.closest('.card');
        if (!card) return;
        const id = card.dataset.id;

        // DELETE
        if (e.target.closest('.delete-btn')) {
            if (!confirm('Удалить товар?')) return;
            try {
                const res = await fetch(`${endpoint}/${id}`, { method: 'DELETE' });
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                await load(activeCategory);
            } catch (err) {
                showError('Не удалось удалить: ' + err.message);
            }
            return;
        }

        // UPDATE
        if (e.target.closest('.edit-btn')) {
            const name = card.querySelector('[data-field="name"]').textContent;
            const category = card.querySelector('[data-field="category"]').textContent;
            const price = card.querySelector('[data-field="price"]').textContent;

            const newName = prompt('Название:', name);
            if (newName === null) return;

            const newCategory = prompt('Категория:', category);
            if (newCategory === null) return;

            const newPriceStr = prompt('Цена:', price.replace(/\s/g, ''));
            if (newPriceStr === null) return;

            const newPrice = parseFloat(newPriceStr);
            if (isNaN(newPrice) || newPrice <= 0) {
                showError('Некорректная цена');
                return;
            }

            try {
                const res = await fetch(`${endpoint}/${id}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify({
                        name: newName,
                        category: newCategory,
                        price: newPrice
                    })
                });
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                await load(activeCategory);
            } catch (err) {
                showError('Не удалось обновить: ' + err.message);
            }
        }
    });

    // ---------- "Подробнее" ----------
    root.addEventListener('click', async e => {
        const btn = e.target.closest('.details-btn');
        if (!btn) return;

        const card = btn.closest('.card');
        const id = card.dataset.id;

        try {
            const res = await fetch(`${endpoint}/${id}`, { headers: { 'Accept': 'application/json' } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const p = await res.json();
            alert(`${p.name}\nКатегория: ${p.category}\nЦена: ${p.price} ₽`);
        } catch (err) {
            showError('Не удалось загрузить товар: ' + err.message);
        }
    });

    load(null);
})();