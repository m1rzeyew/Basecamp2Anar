document.addEventListener('DOMContentLoaded', () => {
    const tabs = document.querySelectorAll('.tab');
    const projectCards = document.querySelectorAll('.project-card');

    tabs.forEach(tab => {
        tab.addEventListener('click', (e) => {
            e.preventDefault();

            // 1. Aktiv klası idarə et
            tabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');

            // 2. Filter dəyərini götür
            const filterValue = tab.getAttribute('data-filter');

            // 3. Kartları göstər və ya gizlə
            projectCards.forEach(card => {
                // Əgər filter 'all'-dursa və ya filter yoxdursa hamısını göstər
                if (filterValue === 'all' || !filterValue) {
                    card.style.display = 'block';
                }
                // Əgər kartın klasları arasında seçilən filter varsa göstər
                else if (card.classList.contains(filterValue)) {
                    card.style.display = 'block';
                }
                // Qalan hallarda gizlə
                else {
                    card.style.display = 'none';
                }
            });
        });
    });


    // Button click animations for edit/delete buttons
    const actionButtons = document.querySelectorAll('.btn-icon');
    actionButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            const projectCard = btn.closest('.project-card');
            if (!projectCard) return;

            const isDelete = btn.classList.contains('btn-delete');
            const action = isDelete ? 'Delete' : 'Edit';
            const projectName = projectCard.querySelector('.project-title').textContent;

            console.log(`${action} clicked for project: ${projectName}`);
        });
    });

    // Project Details Sidebar Toggling
    const sidebarItems = document.querySelectorAll('.sidebar-item');
    const contentSections = document.querySelectorAll('.content-section');

    if (sidebarItems.length > 0 && contentSections.length > 0) {
        sidebarItems.forEach(item => {
            item.addEventListener('click', (e) => {
                const target = item.getAttribute('data-target');
                if (!target) return; // Allow normal link navigation

                e.preventDefault();

                // Update sidebar active state
                sidebarItems.forEach(i => i.classList.remove('active'));
                item.classList.add('active');

                // Update content section visibility
                contentSections.forEach(section => {
                    section.classList.remove('active');
                    if (section.id === target) {
                        section.classList.add('active');
                    }
                });
            });
        });
    }

    // Project task operations are handled by the MVC view so every write can
    // include authorization and antiforgery validation.
});
