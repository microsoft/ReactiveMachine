function navbarBurgerToggle() {
  $('.navbar-burger').click(function() {
    $('.navbar-burger,.navbar-menu').toggleClass('is-active');
  });
}

$(function() {
  console.log("Welcome to the Reactive Machine documentation!");

  navbarBurgerToggle();
});
