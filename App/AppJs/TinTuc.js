
loaddata = function () {
    BaseController.loaddata("TinTuc");
}



deleteData = function (id) {
    BaseController.deleteData("TinTuc", id);
}


editStatus = function (id) {

    BlockUI(); // Không cho người dùng nhập liệu khi đang thao tác với dữ liệu
    $.ajax({
        url: "/TinTuc/Status",
        type: "POST",
        data: { id: id },
        success: function (res) {
            UnBlockUI();
            loaddata();
            if (res.success) {
                showToast(res.message, "success");
            }
            else {
                showToast(res.message, "error");
            }
        },
        error: function (xhr) {
            UnBlockUI();
            if (xhr.status === 401) {
                showToast("Bạn không có quyền truy cập! Vui lòng đăng nhập.");
            } else if (xhr.status === 403) {
                showToast("Bạn không có quyền thực hiện thao tác này!");
            } else {
                showToast("Không thể tải dữ liệu!");
            }
        }
    });
}


loaddata();

