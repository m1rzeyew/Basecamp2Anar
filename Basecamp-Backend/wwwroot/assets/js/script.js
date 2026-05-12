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
            e.preventDefault();
            // Just a visual cue that it was clicked, since there's no backend
            const isDelete = btn.classList.contains('btn-delete');
            const action = isDelete ? 'Delete' : 'Edit';
            const projectName = btn.closest('.project-card').querySelector('.project-title').textContent;

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

    // Tasks Interaction Logic
    const taskList = document.querySelector('.task-list');
    const addTaskBtn = document.querySelector('#tasks .btn-primary');
    const addTaskInput = document.querySelector('#tasks .form-control');

    if (taskList) {
        // Toggle and Delete (using Event Delegation for dynamic items)
        taskList.addEventListener('click', (e) => {
            const taskItem = e.target.closest('.task-item');
            if (!taskItem) return;

            // 1. Delete Action
            if (e.target.closest('.btn-delete-task')) {
                taskItem.classList.add('fade-out');
                taskItem.addEventListener('transitionend', () => {
                    taskItem.remove();
                }, { once: true });
                return;
            }

            // 2. Visual Check Toggle
            if (e.target.closest('.custom-checkbox') || e.target.closest('.task-text')) {
                taskItem.classList.toggle('completed');
                const checkbox = taskItem.querySelector('.custom-checkbox');
                if (taskItem.classList.contains('completed')) {
                    checkbox.innerHTML = '<i class="ph ph-check-bold"></i>';
                } else {
                    checkbox.innerHTML = '';
                }
            }
        });
    }

    if (addTaskBtn && addTaskInput) {
        // 3. Add Task Simulation
        const handleAddTask = () => {
            const taskName = addTaskInput.value.trim();
            if (taskName) {
                const newTask = document.createElement('div');
                newTask.className = 'task-item';
                newTask.innerHTML = `
                    <div class="custom-checkbox"></div>
                    <span class="task-text">${taskName}</span>
                    <div class="btn-delete-task" title="Delete Task"><i class="ph ph-x"></i></div>
                `;

                // Prepend to list
                taskList.prepend(newTask);

                // Clear input
                addTaskInput.value = '';
                addTaskInput.focus();
            }
        };

        addTaskBtn.addEventListener('click', handleAddTask);

        // Also add on Enter key
        addTaskInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                handleAddTask();
            }
        });
    }
});
